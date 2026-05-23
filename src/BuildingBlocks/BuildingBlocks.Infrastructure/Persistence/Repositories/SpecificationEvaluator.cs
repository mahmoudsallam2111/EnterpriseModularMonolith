using BuildingBlocks.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Persistence.Repositories;

/// <summary>
/// Walks an <see cref="ISpecification{T}"/> and applies it to an EF Core queryable —
/// where, includes, ordering, paging, tracking and split-query flags.
/// </summary>
public static class SpecificationEvaluator
{
    public static IQueryable<T> GetQuery<T>(IQueryable<T> input, ISpecification<T> spec) where T : class
    {
        var query = input;

        if (spec.Criteria is not null)
            query = query.Where(spec.Criteria);

        foreach (var include in spec.Includes)
            query = query.Include(include);

        foreach (var include in spec.IncludeStrings)
            query = query.Include(include);

        if (spec.OrderBy is not null)
            query = query.OrderBy(spec.OrderBy);
        else if (spec.OrderByDescending is not null)
            query = query.OrderByDescending(spec.OrderByDescending);

        if (spec.Skip is int s) query = query.Skip(s);
        if (spec.Take is int t) query = query.Take(t);

        if (spec.AsNoTracking) query = query.AsNoTracking();
        if (spec.SplitQuery) query = query.AsSplitQuery();

        return query;
    }
}
