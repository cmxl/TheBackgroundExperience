using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using TheBackgroundExperience.Application.Common;
using TheBackgroundExperience.Application.Common.Interfaces;
using TheBackgroundExperience.Application.Configuration;
using TheBackgroundExperience.Infrastructure.Persistence;
using TheBackgroundExperience.Infrastructure.Persistence.Interceptors;
using TheBackgroundExperience.Infrastructure.Queues;
using TheBackgroundExperience.Infrastructure.Services;

namespace TheBackgroundExperience.Infrastructure;

public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Adds the infrastructure services to the service collection.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <param name="configuration">The configuration.</param>
	/// <returns>The updated service collection.</returns>
	public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
	{
		
		// Register infrastructure services here
		services.AddSingleton<IDateTime, DateTimeService>();
		services.AddPersistence(configuration);
		services.AddRabbitMq();
		
		return services;
	}
	
	private static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddScoped<IInterceptor, DispatchEventsInterceptor>();
		services.AddScoped<IInterceptor, AuditableEntitySaveChangesInterceptor>();
		
		services.AddDbContext<IApplicationDbContext, ApplicationDbContext>((sp, o) =>
		{
			o.UseSqlServer(configuration.GetConnectionString(nameof(ApplicationDbContext)),
			               sqlOptions => sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
			o.AddInterceptors(sp.GetServices<IInterceptor>());
		});
		
		services.AddScoped<ApplicationDbContextInitializer>(provider =>
		{
			var context = provider.GetRequiredService<IApplicationDbContext>();
			var logger = provider.GetRequiredService<ILogger<ApplicationDbContextInitializer>>();
			return new ApplicationDbContextInitializer(logger, (context as ApplicationDbContext)!);
		});	
		return services;
	}
	
	private static IServiceCollection AddRabbitMq(this IServiceCollection services)
	{
		services.AddSingleton<IConnectionFactory>(sp =>
		{
			var options = sp.GetRequiredService<IOptions<RabbitMqConfig>>().Value;
			return new ConnectionFactory
			{
				UserName = options.UserName,
				Password = options.Password,
				Endpoint = new AmqpTcpEndpoint(options.HostName, options.Port)
			};
		});
		
		services.AddSingleton<IQueueManager, QueueManager>();
		
		return services;
	}
}