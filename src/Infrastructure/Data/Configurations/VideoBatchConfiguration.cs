using FiapX.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiapX.Infrastructure.Data.Configurations;

public class VideoBatchConfiguration : IEntityTypeConfiguration<VideoBatch>
{
    public void Configure(EntityTypeBuilder<VideoBatch> builder)
    {
        builder.ToContainer("Videos");

        builder.HasKey(b => b.Id);
        builder.HasPartitionKey(b => b.BatchId);

        builder.Property(b => b.UserOwner)
               .IsRequired();

        builder.Property(b => b.CreatedAt).ToJsonProperty("createdAt");
        builder.Property(b => b.UpdatedAt).ToJsonProperty("updatedAt");

        builder.HasMany(b => b.Videos)
               .WithOne()
               .HasForeignKey(v => v.BatchId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.UseETagConcurrency();
    }
}