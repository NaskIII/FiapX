namespace FiapX.Core.Entities.Base;

public abstract class Entity
{
    public Guid Id { get; protected set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    protected Entity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    protected void RegisterUpdate()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}