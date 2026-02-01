using FiapX.Core.Entities;
using FiapX.Core.Interfaces.Repositories;
using FiapX.Infrastructure.Data;
using System.Diagnostics.CodeAnalysis;

namespace FiapX.Infrastructure.Repositories
{
    [ExcludeFromCodeCoverage]
    public class VideoRepository : Repository<Video>, IVideoRepository
    {
        public VideoRepository(FiapXDbContext context) : base(context)
        {
        }
    }
}
