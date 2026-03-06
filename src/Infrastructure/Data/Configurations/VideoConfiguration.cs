using FiapX.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiapX.Infrastructure.Data.Configurations;

public class VideoConfiguration : IEntityTypeConfiguration<Video>
{
    public void Configure(EntityTypeBuilder<Video> builder)
    {
        builder.ToContainer("Videos");

        builder.HasKey(v => v.Id);

        builder.HasPartitionKey(v => v.BatchId);

        builder.Property(v => v.FileName).IsRequired();
        builder.Property(v => v.FilePath).IsRequired();

        builder.Property(v => v.OutputPath);
        builder.Property(v => v.ErrorMessage);

        builder.Property(v => v.CreatedAt).ToJsonProperty("createdAt");
        builder.Property(v => v.UpdatedAt).ToJsonProperty("updatedAt");

        builder.UseETagConcurrency();
    }
}