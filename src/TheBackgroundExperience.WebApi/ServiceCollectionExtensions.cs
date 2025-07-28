using Serilog;
using TheBackgroundExperience.Application.Common;
using TheBackgroundExperience.WebApi.Filters;
using TheBackgroundExperience.WebApi.Services;

namespace TheBackgroundExperience.WebApi;

public static class ServiceCollectionExtensions
{
	
	public static IServiceCollection AddWebApi(this IServiceCollection services, IConfiguration configuration)
	{
		var redisConnectionString = configuration.GetConnectionString("Redis");
		services.AddStackExchangeRedisOutputCache(x => x.Configuration = redisConnectionString);
		services.AddScoped<ICurrentUserService, CurrentUserService>();
		services.AddOpenApi();
		services.AddControllers(o => o.Filters.Add<ApiExceptionFilterAttribute>());

		return services;
	}
}