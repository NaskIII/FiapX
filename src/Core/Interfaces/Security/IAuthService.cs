using FiapX.Core.Entities;

namespace FiapX.Core.Interfaces.Security
{
    public interface IPasswordHasher
    {
        string Hash(string password);
        bool Verify(string password, string hash);
    }

    public interface ITokenService
    {
        string GenerateToken(User user);
    }
}
