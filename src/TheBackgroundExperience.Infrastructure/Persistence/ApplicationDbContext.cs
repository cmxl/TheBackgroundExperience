using System.Reflection;
using Microsoft.EntityFrameworkCore;
using TheBackgroundExperience.Application.Common.Interfaces;
using TheBackgroundExperience.Domain.Common;
using TheBackgroundExperience.Domain.Entities;

namespace TheBackgroundExperience.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
	public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
		: base(options)
	{
	}
	
	public DbSet<Student> Students { get; set; }
	
	protected override void OnModelCreating(ModelBuilder builder)
	{
		builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

		var baseEntities = builder.Model.GetEntityTypes()
		                          .Select(x => x.ClrType)
		                          .Where(x => typeof(BaseAuditableEntity).IsAssignableFrom(x));

		foreach (var entity in baseEntities)
		{
			builder.Entity(entity).Property(nameof(BaseAuditableEntity.Created)).HasColumnName("created").HasDefaultValueSql("SYSDATETIMEOFFSET()");
			builder.Entity(entity).Property(nameof(BaseAuditableEntity.CreatedBy)).HasColumnName("created_by").HasMaxLength(36).HasDefaultValueSql("APP_NAME()");
			builder.Entity(entity).Property(nameof(BaseAuditableEntity.LastModified)).HasColumnName("last_modified").HasDefaultValueSql("SYSDATETIMEOFFSET()");
			builder.Entity(entity).Property(nameof(BaseAuditableEntity.LastModifiedBy)).HasColumnName("last_modified_by").HasMaxLength(36).HasDefaultValueSql("APP_NAME()");
		}

		base.OnModelCreating(builder);
	}
}