using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace TheBackgroundExperience.Infrastructure.Persistence;

public class ApplicationDbContextInitializer
{
	private readonly ApplicationDbContext _context;
	private readonly ILogger<ApplicationDbContextInitializer> _logger;

	public ApplicationDbContextInitializer(
		ILogger<ApplicationDbContextInitializer> logger, 
		ApplicationDbContext context)
	{
		_logger = logger;
		_context = context;
	}

	public async Task InitialiseAsync()
	{
		try
		{
			if (_context.Database.IsSqlServer()) 
			{
				await _context.Database.EnsureCreatedAsync();
				//await _context.Database.MigrateAsync();
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "An error occurred while initialising the database");
		}
	}

	public async Task SeedAsync()
	{
		try
		{
			await TrySeedAsync();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "An error occurred while seeding the database");
			throw;
		}
	}

	public async Task TrySeedAsync()
	{
		await Task.CompletedTask;
	}
}