namespace BuildingBlocks.Application.DataFiltering;

/// <summary>
/// ABP-style data filter service. Filters are addressed by a marker type
/// (typically <c>BuildingBlocks.Domain.ISoftDeletable</c>,
/// <c>BuildingBlocks.Domain.IMultiTenantEntity</c>, or any custom marker
/// you introduce). State is held in AsyncLocal so concurrent requests
/// don't see each other's overrides.
/// </summary>
///
/// <example>
/// using (_dataFilter.Disable&lt;ISoftDeletable&gt;())
/// {
///     // queries inside this block see soft-deleted rows too
///     var all = await repo.ListAsync(ct);
/// }
/// // filter is restored to its previous state automatically
/// </example>
public interface IDataFilter
{
    /// <summary>
    /// Disables the filter identified by <typeparamref name="TFilter"/> for the
    /// duration of the returned <see cref="IDisposable"/>. Nesting is honoured:
    /// disposing restores the previous (possibly disabled) state.
    /// </summary>
    IDisposable Disable<TFilter>() where TFilter : class;

    /// <summary>Counterpart to <see cref="Disable{TFilter}"/>.</summary>
    IDisposable Enable<TFilter>() where TFilter : class;

    /// <summary>
    /// Whether the filter is currently active for this async flow.
    /// Default: <c>true</c> (filters enabled out of the box).
    /// </summary>
    bool IsEnabled<TFilter>() where TFilter : class;

    /// <summary>Non-generic variant — useful when the filter type is only known at runtime.</summary>
    bool IsEnabled(Type filterMarker);
}

/// <summary>
/// Typed shortcut around <see cref="IDataFilter"/> for a specific filter marker.
/// Lets you inject <c>IDataFilter&lt;ISoftDeletable&gt;</c> and call <c>.Disable()</c>
/// without restating the generic parameter at every use site.
/// </summary>
public interface IDataFilter<TFilter> where TFilter : class
{
    IDisposable Disable();
    IDisposable Enable();
    bool IsEnabled { get; }
}
