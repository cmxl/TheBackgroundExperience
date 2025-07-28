using System.ComponentModel.DataAnnotations.Schema;
using TheBackgroundExperience.Domain.Common.Caching;

namespace TheBackgroundExperience.Domain.Entities;

public class Student : Person, ICachable
{
	[NotMapped]
	public string CacheKey => StudentCacheKeys.GetCacheKey(Id);

	[NotMapped] 
	public bool BypassCache { get; set; }
}