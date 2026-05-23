using BuildingBlocks.SharedKernel;
using Customers.Application.Customers.Queries;
using Customers.Application.Customers.Queries.GetCustomer;
using Microsoft.EntityFrameworkCore;

namespace Customers.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of the read model. AsNoTracking everywhere, projects directly
/// into the DTOs so there's no over-fetching. If you split read/write stores later,
/// this is the seam that switches to the other store.
/// </summary>
internal sealed class CustomerReadModel : ICustomerReadModel
{
    private readonly CustomersDbContext _context;
    public CustomerReadModel(CustomersDbContext context) => _context = context;

    public Task<CustomerDetailsDto?> GetByIdAsync(Guid customerId, CancellationToken cancellationToken) =>
        _context.Customers
            .AsNoTracking()
            .Where(c => c.Id.Value == customerId)
            .Select(c => new CustomerDetailsDto(
                c.Id.Value,
                c.Name.First,
                c.Name.Last,
                c.Email.Value,
                c.Status.ToString(),
                c.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PagedList<CustomerDetailsDto>> ListAsync(
        string? search, string? status, PageRequest page, CancellationToken cancellationToken)
    {
        var query = _context.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            query = query.Where(c =>
                EF.Functions.ILike(c.Email.Value, $"%{s}%") ||
                EF.Functions.ILike(c.Name.First, $"%{s}%") ||
                EF.Functions.ILike(c.Name.Last, $"%{s}%"));
        }

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<Domain.Customers.CustomerStatus>(status, true, out var parsed))
        {
            query = query.Where(c => c.Status == parsed);
        }

        var total = await query.LongCountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip(page.Skip)
            .Take(page.Take)
            .Select(c => new CustomerDetailsDto(
                c.Id.Value,
                c.Name.First,
                c.Name.Last,
                c.Email.Value,
                c.Status.ToString(),
                c.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedList<CustomerDetailsDto>(items, page.Page, page.Take, total);
    }
}
