using FiapX.Core.Entities;
using FiapX.Core.Interfaces.Repositories;
using FiapX.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FiapX.Infrastructure.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(FiapXDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        var count = await _dbSet
        .Where(u => u.Email == email)
        .CountAsync();

        return count > 0;
    }
}