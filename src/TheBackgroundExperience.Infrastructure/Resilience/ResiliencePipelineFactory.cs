using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;
using Polly.Retry;
using Polly.Timeout;
using TheBackgroundExperience.Application.Configuration;

namespace TheBackgroundExperience.Infrastructure.Resilience;

public class ResiliencePipelineFactory
{
	private readonly ResilienceConfig _config;
	private readonly ILogger<ResiliencePipelineFactory> _logger;
	private readonly ResiliencePipelineRegistry<string> _registry;

	public ResiliencePipelineFactory(
		IOptions<ResilienceConfig> config,
		ILogger<ResiliencePipelineFactory> logger)
	{
		_config = config.Value;
		_logger = logger;
		_registry = new ResiliencePipelineRegistry<string>();
		InitializePipelines();
	}

	private void InitializePipelines()
	{
		// RabbitMQ Pipeline
		_registry.TryAddBuilder("rabbitmq", (builder, context) =>
		{
			var config = _config.RabbitMQ;
			builder
				.AddRetry(new RetryStrategyOptions
				{
					ShouldHandle = new PredicateBuilder().Handle<Exception>(),
					MaxRetryAttempts = config.MaxRetryAttempts,
					Delay = config.BaseDelay,
					MaxDelay = config.MaxDelay,
					BackoffType = DelayBackoffType.Exponential,
					UseJitter = true,
					OnRetry = args =>
					{
						_logger.LogWarning("RabbitMQ operation retry {AttemptNumber}/{MaxAttempts}: {Exception}",
							args.AttemptNumber, config.MaxRetryAttempts, args.Outcome.Exception?.Message);
						return ValueTask.CompletedTask;
					}
				})
				.AddCircuitBreaker(new CircuitBreakerStrategyOptions
				{
					ShouldHandle = new PredicateBuilder().Handle<Exception>(),
					FailureRatio = config.FailureRatio,
					SamplingDuration = TimeSpan.FromSeconds(config.SamplingDuration),
					MinimumThroughput = config.MinimumThroughput,
					BreakDuration = config.DurationOfBreak,
					OnOpened = args =>
					{
						_logger.LogError("RabbitMQ circuit breaker opened: {Exception}", args.Outcome.Exception?.Message);
						return ValueTask.CompletedTask;
					},
					OnClosed = args =>
					{
						_logger.LogInformation("RabbitMQ circuit breaker closed");
						return ValueTask.CompletedTask;
					},
					OnHalfOpened = args =>
					{
						_logger.LogInformation("RabbitMQ circuit breaker half-opened");
						return ValueTask.CompletedTask;
					}
				})
				.AddTimeout(config.Timeout);
		});

		// Redis Pipeline
		_registry.TryAddBuilder("redis", (builder, context) =>
		{
			var config = _config.Redis;
			builder
				.AddRetry(new RetryStrategyOptions
				{
					ShouldHandle = new PredicateBuilder().Handle<Exception>(),
					MaxRetryAttempts = config.MaxRetryAttempts,
					Delay = config.BaseDelay,
					MaxDelay = config.MaxDelay,
					BackoffType = DelayBackoffType.Exponential,
					UseJitter = true,
					OnRetry = args =>
					{
						_logger.LogWarning("Redis operation retry {AttemptNumber}/{MaxAttempts}: {Exception}",
							args.AttemptNumber, config.MaxRetryAttempts, args.Outcome.Exception?.Message);
						return ValueTask.CompletedTask;
					}
				})
				.AddCircuitBreaker(new CircuitBreakerStrategyOptions
				{
					ShouldHandle = new PredicateBuilder().Handle<Exception>(),
					FailureRatio = config.FailureRatio,
					SamplingDuration = TimeSpan.FromSeconds(config.SamplingDuration),
					MinimumThroughput = config.MinimumThroughput,
					BreakDuration = config.DurationOfBreak,
					OnOpened = args =>
					{
						_logger.LogError("Redis circuit breaker opened: {Exception}", args.Outcome.Exception?.Message);
						return ValueTask.CompletedTask;
					},
					OnClosed = args =>
					{
						_logger.LogInformation("Redis circuit breaker closed");
						return ValueTask.CompletedTask;
					},
					OnHalfOpened = args =>
					{
						_logger.LogInformation("Redis circuit breaker half-opened");
						return ValueTask.CompletedTask;
					}
				})
				.AddTimeout(config.Timeout);
		});

		// Database Pipeline
		_registry.TryAddBuilder("database", (builder, context) =>
		{
			var config = _config.Database;
			builder
				.AddRetry(new RetryStrategyOptions
				{
					ShouldHandle = new PredicateBuilder().Handle<Exception>(),
					MaxRetryAttempts = config.MaxRetryAttempts,
					Delay = config.BaseDelay,
					MaxDelay = config.MaxDelay,
					BackoffType = DelayBackoffType.Exponential,
					UseJitter = true,
					OnRetry = args =>
					{
						_logger.LogWarning("Database operation retry {AttemptNumber}/{MaxAttempts}: {Exception}",
							args.AttemptNumber, config.MaxRetryAttempts, args.Outcome.Exception?.Message);
						return ValueTask.CompletedTask;
					}
				})
				.AddCircuitBreaker(new CircuitBreakerStrategyOptions
				{
					ShouldHandle = new PredicateBuilder().Handle<Exception>(),
					FailureRatio = config.FailureRatio,
					SamplingDuration = TimeSpan.FromSeconds(config.SamplingDuration),
					MinimumThroughput = config.MinimumThroughput,
					BreakDuration = config.DurationOfBreak,
					OnOpened = args =>
					{
						_logger.LogError("Database circuit breaker opened: {Exception}", args.Outcome.Exception?.Message);
						return ValueTask.CompletedTask;
					},
					OnClosed = args =>
					{
						_logger.LogInformation("Database circuit breaker closed");
						return ValueTask.CompletedTask;
					},
					OnHalfOpened = args =>
					{
						_logger.LogInformation("Database circuit breaker half-opened");
						return ValueTask.CompletedTask;
					}
				})
				.AddTimeout(config.Timeout);
		});
	}

	public ResiliencePipeline GetPipeline(string key)
	{
		return _registry.GetPipeline(key);
	}

	public ResiliencePipeline<T> GetPipeline<T>(string key)
	{
		return _registry.GetPipeline<T>(key);
	}
}