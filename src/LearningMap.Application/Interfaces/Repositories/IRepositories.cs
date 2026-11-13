using System.Linq.Expressions;
using LearningMap.Application.Dtos;
using LearningMap.Domain.Entities;

namespace LearningMap.Application.Interfaces.Repositories;

/// <summary>Generic CRUD every repository gets for free (same idea as SafeSpace's IGenericRepository).</summary>
public interface IGenericRepository<T> where T : class
{
    Task<T?> GetAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Remove(T entity);
}

public interface IUserRepository : IGenericRepository<AppUser>
{
    Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}

public interface IEmployeeRepository : IGenericRepository<Employee>
{
    Task<Employee?> GetByGuidAsync(Guid employeeGuid, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetPagedAsync(
        string? search, string? department, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}

/// <summary>
/// Unit of Work: groups the repositories behind one SaveChangesAsync, so several changes
/// are committed together in ONE database transaction (or not at all).
/// </summary>
public interface IUnitOfWork
{
    IUserRepository Users { get; }
    IEmployeeRepository Employees { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Read-only reporting queries written with Dapper + stored procedures,
/// the way SafeSpace.SeniorLiving.Api's DataAccess providers work.
/// </summary>
public interface IReportRepository
{
    Task<IReadOnlyList<DepartmentSummaryDto>> GetDepartmentSummaryAsync(CancellationToken cancellationToken = default);
}
