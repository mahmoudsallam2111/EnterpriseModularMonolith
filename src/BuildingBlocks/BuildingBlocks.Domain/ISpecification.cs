using System.Linq.Expressions;

namespace BuildingBlocks.Domain;

/// <summary>
/// Specification pattern. Captures a queryable criterion plus optional includes,
/// ordering, paging, and projection hints — composable with And/Or/Not.
/// </summary>
public interface ISpecification<T>
{
    Expression<Func<T, bool>>? Criteria { get; }
    IReadOnlyList<Expression<Func<T, object>>> Includes { get; }
    IReadOnlyList<string> IncludeStrings { get; }
    Expression<Func<T, object>>? OrderBy { get; }
    Expression<Func<T, object>>? OrderByDescending { get; }
    int? Take { get; }
    int? Skip { get; }
    bool AsNoTracking { get; }
    bool SplitQuery { get; }
}

public abstract class Specification<T> : ISpecification<T>
{
    private readonly List<Expression<Func<T, object>>> _includes = [];
    private readonly List<string> _includeStrings = [];

    protected Specification(Expression<Func<T, bool>>? criteria = null) => Criteria = criteria;

    public Expression<Func<T, bool>>? Criteria { get; private set; }
    public IReadOnlyList<Expression<Func<T, object>>> Includes => _includes;
    public IReadOnlyList<string> IncludeStrings => _includeStrings;
    public Expression<Func<T, object>>? OrderBy { get; private set; }
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }
    public int? Take { get; private set; }
    public int? Skip { get; private set; }
    public bool AsNoTracking { get; private set; }
    public bool SplitQuery { get; private set; }

    protected void AddInclude(Expression<Func<T, object>> include) => _includes.Add(include);
    protected void AddInclude(string include) => _includeStrings.Add(include);
    protected void ApplyOrderBy(Expression<Func<T, object>> orderBy) => OrderBy = orderBy;
    protected void ApplyOrderByDescending(Expression<Func<T, object>> orderBy) => OrderByDescending = orderBy;
    protected void ApplyPaging(int skip, int take) { Skip = skip; Take = take; }
    protected void DisableTracking() => AsNoTracking = true;
    protected void EnableSplitQuery() => SplitQuery = true;
}
