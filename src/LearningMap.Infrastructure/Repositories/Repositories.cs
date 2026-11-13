using System.Data;
using System.Linq.Expressions;
using Dapper;
using LearningMap.Application.Dtos;
using LearningMap.Application.Interfaces.Repositories;
using LearningMap.Domain.Entities;
using LearningMap.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LearningMap.Infrastructure.Repositories;

public abstract class GenericRepository<T>(LearningMapDbContext context) : IGenericRepository<T> where T : class
{
    protected readonly LearningMapDbContext Context = context;

    public Task<T?> GetAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default) =>
        Context.Set<T>().FirstOrDefaultAsync(filter, cancellationToken);

    public async Task<IReadOnlyList<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null, CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = Context.Set<T>().AsNoTracking();
        if (filter is not null) query = query.Where(filter);
        return await query.ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default) =>
        Context.Set<T>().AnyAsync(filter, cancellationToken);

    // NOTE: SafeSpace's GenericRepository has `public async void Create(...)`. `async void` cannot be
    // awaited and its exceptions crash the process, so here it returns a Task.
    public async Task AddAsync(T entity, CancellationToken cancellationToken = default) =>
        await Context.Set<T>().AddAsync(entity, cancellationToken);

    public void Update(T entity) => Context.Set<T>().Update(entity);

    public void Remove(T entity) => Context.Set<T>().Remove(entity);
}

public class UserRepository(LearningMapDbContext context) : GenericRepository<AppUser>(context), IUserRepository
{
    public Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
}

public class EmployeeRepository(LearningMapDbContext context) : GenericRepository<Employee>(context), IEmployeeRepository
{
    public Task<Employee?> GetByGuidAsync(Guid employeeGuid, CancellationToken cancellationToken = default) =>
        Context.Employees.FirstOrDefaultAsync(e => e.EmployeeGuid == employeeGuid, cancellationToken);

    public async Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetPagedAsync(
        string? search, string? department, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        // IQueryable builds up SQL; nothing hits the database until CountAsync / ToListAsync.
        IQueryable<Employee> query = Context.Employees.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e =>
                e.EmployeeCode.Contains(term) ||
                e.FirstName.Contains(term) || e.LastName.Contains(term) ||
                e.Email.Contains(term) || e.Designation.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(department))
        {
            var dept = department.Trim();
            query = query.Where(e => e.Department == dept);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(e => e.FirstName).ThenBy(e => e.LastName).ThenBy(e => e.EmployeeId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}

/// <summary>
/// Dapper: you write the SQL (here: call a stored procedure), Dapper maps result columns to properties.
/// It reuses EF's connection, so there is one connection string and one connection per request.
/// </summary>
public class ReportRepository(LearningMapDbContext context) : IReportRepository
{
    public async Task<IReadOnlyList<DepartmentSummaryDto>> GetDepartmentSummaryAsync(CancellationToken cancellationToken = default)
    {
        var connection = context.Database.GetDbConnection();
        var rows = await connection.QueryAsync<DepartmentSummaryDto>(new CommandDefinition(
            "dbo.usp_GetDepartmentSummary",
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));
        return rows.AsList();
    }
}

public class UnitOfWork(LearningMapDbContext context) : IUnitOfWork
{
    // Lazily created, same pattern as SafeSpace's UnitOfWork. All share ONE DbContext,
    // which is what makes SaveChangesAsync a single transaction across repositories.
    private UserRepository? _users;
    private EmployeeRepository? _employees;

    public IUserRepository Users => _users ??= new UserRepository(context);
    public IEmployeeRepository Employees => _employees ??= new EmployeeRepository(context);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
