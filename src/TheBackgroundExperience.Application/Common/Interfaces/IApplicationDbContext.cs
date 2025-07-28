using Microsoft.EntityFrameworkCore;
using TheBackgroundExperience.Domain.Entities;

namespace TheBackgroundExperience.Application.Common.Interfaces;

public interface IApplicationDbContext
{
	DbSet<Student> Students { get; }
	
	Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}