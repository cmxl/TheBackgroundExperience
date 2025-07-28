using System.Text.Json;
using Mediator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheBackgroundExperience.Application.Common.Behaviours;
using TheBackgroundExperience.Application.Configuration;
using TheBackgroundExperience.Application.Students.Commands;
using TheBackgroundExperience.Domain.Events;
using ZiggyCreatures.Caching.Fusion;

namespace TheBackgroundExperience.Application;

public static class ServiceCollectionExtensions
{
	/// <summary>
	///     Adds the application services to the service collection.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <returns>The updated service collection.</returns>
	public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
	{
		// Register application services here
		services.AddOptions();
		services.Configure<RabbitMqConfig>(configuration.GetSection(RabbitMqConfig.SectionName));
		
		services.AddMediator(o =>
		{
			o.Assemblies = [typeof(CreateStudentCommand), typeof(StudentUpdatedEvent)];
			o.NotificationPublisherType = typeof(ForeachAwaitPublisher);
			o.ServiceLifetime = ServiceLifetime.Scoped;
			o.PipelineBehaviors = [typeof(CachingBehaviour<,>)];
		});

		services.AddMemoryCache();
		var redisConnectionString = configuration.GetConnectionString("Redis");
		services.AddStackExchangeRedisCache(x =>
		{
			x.Configuration = redisConnectionString;
			x.InstanceName = "TheBackgroundExperience";
		});
		services.AddFusionCacheStackExchangeRedisBackplane(x => { x.Configuration = redisConnectionString; });
		services.AddFusionCacheSystemTextJsonSerializer(new JsonSerializerOptions(JsonSerializerDefaults.Web));
		services.AddFusionCache()
		        .TryWithAutoSetup()
		        .WithOptions(o =>
		        {
			        o.CacheKeyPrefix = "TheBackgroundExperience";
			        o.EnableAutoRecovery = true;
		        })
		        .WithDefaultEntryOptions(x => x.SetSkipMemoryCache()
		                                       .SetDuration(TimeSpan.FromMinutes(15))
		                                       .SetJittering(TimeSpan.FromSeconds(1)))
		        .WithRegisteredSerializer()
		        .TryWithRegisteredBackplane();
		return services;
	}
}