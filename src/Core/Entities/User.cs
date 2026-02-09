using FiapX.Core.Entities.Base;

namespace FiapX.Core.Entities;

public class User : Entity
{
    public Guid UserId { get; private set; }
    public string Username { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }

    protected User() { }

    public User(string username, string email, string passwordHash)
    {
        Username = username;
        Email = email;
        PasswordHash = passwordHash;
    }
}