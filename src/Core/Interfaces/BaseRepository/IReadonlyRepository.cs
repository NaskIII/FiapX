using System.Linq.Expressions;

namespace FiapX.Core.Interfaces.BaseRepository
{
    public interface IReadonlyRepository<T> : IDisposable where T : class
    {
        Task<T?> GetByIdAsync(params object[] ids);
        Task<List<T>> GetManyByIdAsync<TId>(List<TId> ids);
        Task<List<T>> AsNoTrackingListAsync();

        Task<T?> GetSingleAsync(Expression<Func<T, bool>> query);
        Task<T?> GetSingleAsync(Expression<Func<T, bool>> query, params string[] joins);
        Task<T?> GetSingleAsync(Expression<Func<T, bool>> query, params Expression<Func<T, object>>[] joins);

        Task<List<T>> GetManyAsync(Expression<Func<T, bool>> query);
        Task<List<T>> GetManyAsync(Expression<Func<T, bool>> query, params string[] joins);
        Task<List<T>> GetManyAsync(Expression<Func<T, bool>> query, params Expression<Func<T, object>>[] joins);

        Task<int> CountAsync(Expression<Func<T, bool>> query, params string[] joins);
        Task<int> CountAsync();

        Task<bool> ExistsByIdAsync(params object[] ids);
        Task<bool> ExistsAsync(Expression<Func<T, bool>> query);

        Task<bool> IsActiveAsync(params object[] ids);

        Task<List<T>> GetPagedDataAsync(Expression<Func<T, bool>> query, int pageNumber, int pageSize, string orderByProperty, bool orderByAscending, params string[] joins);

        IQueryable<T> AsQueryable();
    }
}
