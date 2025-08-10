using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;

namespace TheBackgroundExperience.WebApi.Middleware;

public class RequestDeduplicationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestDeduplicationMiddleware> _logger;
    private static readonly ConcurrentDictionary<string, TaskCompletionSource<CachedResponse>> _pendingRequests = new();
    private static readonly Timer _cleanupTimer;

    static RequestDeduplicationMiddleware()
    {
        _cleanupTimer = new Timer(CleanupExpiredRequests, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public RequestDeduplicationMiddleware(RequestDelegate next, ILogger<RequestDeduplicationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.Features.Get<IEndpointFeature>()?.Endpoint;
        var deduplicateAttribute = endpoint?.Metadata.GetMetadata<DeduplicateRequestAttribute>();
        
        if (deduplicateAttribute == null || !ShouldDeduplicate(context))
        {
            await _next(context);
            return;
        }

        var requestKey = await GenerateRequestKey(context);
        _logger.LogDebug("Request key generated: {RequestKey}", requestKey);

        if (_pendingRequests.TryGetValue(requestKey, out var existingTcs))
        {
            _logger.LogInformation("Coalescing duplicate request for key: {RequestKey}", requestKey);
            var cachedResponse = await existingTcs.Task;
            await WriteCachedResponse(context, cachedResponse);
            return;
        }

        var tcs = new TaskCompletionSource<CachedResponse>();
        if (!_pendingRequests.TryAdd(requestKey, tcs))
        {
            var existingTcs2 = _pendingRequests[requestKey];
            var cachedResponse2 = await existingTcs2.Task;
            await WriteCachedResponse(context, cachedResponse2);
            return;
        }

        try
        {
            var originalStream = context.Response.Body;
            using var responseStream = new MemoryStream();
            context.Response.Body = responseStream;

            await _next(context);

            responseStream.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(responseStream).ReadToEndAsync();
            
            var cachedResponse = new CachedResponse
            {
                StatusCode = context.Response.StatusCode,
                ContentType = context.Response.ContentType,
                Body = responseBody,
                Headers = context.Response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
                ExpiryTime = DateTime.UtcNow.Add(deduplicateAttribute.CacheDuration)
            };

            responseStream.Seek(0, SeekOrigin.Begin);
            await responseStream.CopyToAsync(originalStream);
            context.Response.Body = originalStream;

            tcs.SetResult(cachedResponse);
            _logger.LogDebug("Request completed and cached for key: {RequestKey}", requestKey);
        }
        catch (Exception ex)
        {
            tcs.SetException(ex);
            throw;
        }
        finally
        {
            _pendingRequests.TryRemove(requestKey, out _);
        }
    }

    private static bool ShouldDeduplicate(HttpContext context)
    {
        return context.Request.Method == "GET" || context.Request.Method == "POST";
    }

    private static async Task<string> GenerateRequestKey(HttpContext context)
    {
        var sb = new StringBuilder();
        sb.Append(context.Request.Method);
        sb.Append('|');
        sb.Append(context.Request.Path);
        
        if (context.Request.QueryString.HasValue)
        {
            sb.Append(context.Request.QueryString.Value);
        }
        
        if (context.Request.Method == "POST" && context.Request.ContentLength > 0)
        {
            context.Request.EnableBuffering();
            var bodyPosition = context.Request.Body.Position;
            context.Request.Body.Seek(0, SeekOrigin.Begin);
            
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            sb.Append('|');
            sb.Append(body);
            
            context.Request.Body.Seek(bodyPosition, SeekOrigin.Begin);
        }

        var key = sb.ToString();
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
        return Convert.ToBase64String(hash);
    }

    private async Task WriteCachedResponse(HttpContext context, CachedResponse cachedResponse)
    {
        context.Response.StatusCode = cachedResponse.StatusCode;
        context.Response.ContentType = cachedResponse.ContentType;
        
        foreach (var header in cachedResponse.Headers)
        {
            if (!context.Response.Headers.ContainsKey(header.Key))
            {
                context.Response.Headers[header.Key] = header.Value;
            }
        }

        context.Response.Headers["X-Deduplicated"] = "true";
        
        if (!string.IsNullOrEmpty(cachedResponse.Body))
        {
            await context.Response.WriteAsync(cachedResponse.Body);
        }
    }

    private static void CleanupExpiredRequests(object? state)
    {
        var expiredKeys = new List<string>();
        foreach (var kvp in _pendingRequests)
        {
            if (kvp.Value.Task.IsCompleted)
            {
                if (kvp.Value.Task.Result.ExpiryTime < DateTime.UtcNow)
                {
                    expiredKeys.Add(kvp.Key);
                }
            }
        }

        foreach (var key in expiredKeys)
        {
            _pendingRequests.TryRemove(key, out _);
        }
    }
}

public class DeduplicateRequestAttribute : Attribute
{
    public TimeSpan CacheDuration { get; }

    public DeduplicateRequestAttribute(int cacheDurationSeconds = 30)
    {
        CacheDuration = TimeSpan.FromSeconds(cacheDurationSeconds);
    }
}

public class CachedResponse
{
    public int StatusCode { get; set; }
    public string? ContentType { get; set; }
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
    public DateTime ExpiryTime { get; set; }
}