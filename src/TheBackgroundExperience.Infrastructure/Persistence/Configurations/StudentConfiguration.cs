using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheBackgroundExperience.Domain.Entities;

namespace TheBackgroundExperience.Infrastructure.Persistence.Configurations;

internal class StudentConfiguration : IEntityTypeConfiguration<Student>
{
	public void Configure(EntityTypeBuilder<Student> builder)
	{
		builder.ToTable("students");
		builder.HasKey(s => s.Id);
		builder.Property(s => s.Id)
			.HasColumnName("id")
			.ValueGeneratedOnAdd();
		builder.Property(s => s.FirstName)
			.HasColumnName("first_name");
		builder.Property(s => s.LastName)
			.HasColumnName("last_name");
		builder.Property(s => s.Updates)
		       .HasColumnName("updates");
	}
}