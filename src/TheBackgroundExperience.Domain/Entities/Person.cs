using System.ComponentModel.DataAnnotations.Schema;

namespace TheBackgroundExperience.Domain.Entities;

public class Person : BaseAuditableEntity<Guid>
{
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
	public int Updates { get; set; }
	
	[NotMapped]
	public string FullName => $"{FirstName} {LastName}";
}