using FiapX.Core.Entities;
using FiapX.Core.Interfaces.BaseRepository;

namespace FiapX.Core.Interfaces.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<bool> EmailExistsAsync(string email);
}