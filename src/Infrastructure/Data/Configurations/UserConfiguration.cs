using FiapX.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiapX.Infrastructure.Data.Configurations
{
    internal class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToContainer("Users");

            builder.HasKey(b => b.Id);
            builder.HasPartitionKey(b => b.UserId);

            builder.Property(b => b.Username)
                   .IsRequired();
            builder.Property(b => b.Email)
                   .IsRequired();

            builder.HasIndex(b => b.Email)
                   .IsUnique();
            builder.HasIndex(b => b.Username)
                   .IsUnique();

            builder.Property(b => b.CreatedAt).ToJsonProperty("createdAt");
            builder.Property(b => b.UpdatedAt).ToJsonProperty("updatedAt");


            builder.UseETagConcurrency();
        }
    }
}
