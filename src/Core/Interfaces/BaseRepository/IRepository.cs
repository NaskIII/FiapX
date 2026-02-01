namespace FiapX.Core.Interfaces.BaseRepository
{
    public interface IRepository<T> : IReadonlyRepository<T> where T : class
    {
        Task AddAsync(T entity);
        void Update(T entity);
        void Delete(T entity);
    }
}