using System.Linq.Expressions;
using FiapX.Core.Interfaces.BaseRepository;
using FiapX.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FiapX.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly FiapXDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(FiapXDbContext context)
    {
        _context = context;
        _dbSet = _context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(params object[] ids)
    {
        return await _dbSet.FindAsync(ids);
    }

    public async Task<List<T>> GetManyByIdAsync<TId>(List<TId> ids)
    {
        return await _dbSet.Where(e => ids.Contains(EF.Property<TId>(e, "Id"))).ToListAsync();
    }

    public async Task<List<T>> AsNoTrackingListAsync()
    {
        return await _dbSet.AsNoTracking().ToListAsync();
    }

    public async Task<T?> GetSingleAsync(Expression<Func<T, bool>> query)
    {
        return await _dbSet.FirstOrDefaultAsync(query);
    }

    public async Task<T?> GetSingleAsync(Expression<Func<T, bool>> query, params string[] joins)
    {
        var queryable = _dbSet.AsQueryable();
        foreach (var join in joins)
        {
            queryable = queryable.Include(join);
        }
        return await queryable.FirstOrDefaultAsync(query);
    }

    public async Task<T?> GetSingleAsync(Expression<Func<T, bool>> query, params Expression<Func<T, object>>[] joins)
    {
        var queryable = _dbSet.AsQueryable();
        foreach (var join in joins)
        {
            queryable = queryable.Include(join);
        }
        return await queryable.FirstOrDefaultAsync(query);
    }

    public async Task<List<T>> GetManyAsync(Expression<Func<T, bool>> query)
    {
        return await _dbSet.Where(query).ToListAsync();
    }

    public async Task<List<T>> GetManyAsync(Expression<Func<T, bool>> query, params string[] joins)
    {
        var queryable = _dbSet.Where(query);
        foreach (var join in joins)
        {
            queryable = queryable.Include(join);
        }
        return await queryable.ToListAsync();
    }

    public async Task<List<T>> GetManyAsync(Expression<Func<T, bool>> query, params Expression<Func<T, object>>[] joins)
    {
        var queryable = _dbSet.Where(query);
        foreach (var join in joins)
        {
            queryable = queryable.Include(join);
        }
        return await queryable.ToListAsync();
    }

    public async Task<int> CountAsync(Expression<Func<T, bool>> query, params string[] joins)
    {
        return await _dbSet.CountAsync(query);
    }

    public async Task<int> CountAsync()
    {
        return await _dbSet.CountAsync();
    }

    public async Task<bool> ExistsByIdAsync(params object[] ids)
    {
        return await _dbSet.FindAsync(ids) != null;
    }

    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> query)
    {
        return await _dbSet.AnyAsync(query);
    }

    public async Task<bool> IsActiveAsync(params object[] ids)
    {
        var entity = await _dbSet.FindAsync(ids);
        if (entity == null) return false;

        var prop = entity.GetType().GetProperty("Active") ?? entity.GetType().GetProperty("IsActive");
        if (prop != null && prop.PropertyType == typeof(bool))
        {
            return (bool)prop.GetValue(entity)!;
        }

        return true;
    }

    public async Task<List<T>> GetPagedDataAsync(Expression<Func<T, bool>> query, int pageNumber, int pageSize, string orderByProperty, bool orderByAscending, params string[] joins)
    {
        var queryable = _dbSet.Where(query);

        // Aplica Includes
        foreach (var join in joins)
        {
            queryable = queryable.Include(join);
        }

        queryable = orderByAscending
            ? queryable.OrderBy(e => EF.Property<object>(e, orderByProperty))
            : queryable.OrderByDescending(e => EF.Property<object>(e, orderByProperty));

        return await queryable
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public IQueryable<T> AsQueryable()
    {
        return _dbSet.AsQueryable();
    }

    // --- IRepository Implementation ---

    public async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
    }

    public void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    public void Delete(T entity)
    {
        _dbSet.Remove(entity);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}