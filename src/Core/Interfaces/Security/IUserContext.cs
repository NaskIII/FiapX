namespace FiapX.Core.Interfaces.Security
{
    public interface IUserContext
    {
        Guid UserId { get; }
        bool IsAuthenticated { get; }
    }
}
