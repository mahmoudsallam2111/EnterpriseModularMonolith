using System.Collections.Concurrent;

namespace BuildingBlocks.Application.DataFiltering;

/// <summary>
/// AsyncLocal-backed <see cref="IDataFilter"/>. Each known filter marker keeps a
/// stack of enable/disable scopes; the head of the stack is the current state.
/// Disposing the IDisposable returned by <c>Enable</c>/<c>Disable</c> pops the head,
/// restoring the previous state. Default is "enabled" if no scope has been opened.
/// </summary>
public sealed class AmbientDataFilter : IDataFilter
{
    private static readonly AsyncLocal<ConcurrentDictionary<Type, Stack<bool>>> Stacks = new();

    public IDisposable Disable<TFilter>() where TFilter : class => Push(typeof(TFilter), false);
    public IDisposable Enable<TFilter>() where TFilter : class => Push(typeof(TFilter), true);

    public bool IsEnabled<TFilter>() where TFilter : class => IsEnabled(typeof(TFilter));

    // The current state is always stack.Peek().
    public bool IsEnabled(Type filterMarker)
    {
        var stacks = Stacks.Value;
        if (stacks is null) return true;
        if (!stacks.TryGetValue(filterMarker, out var stack)) return true;
        lock (stack)
        {
            return stack.Count == 0 || stack.Peek();
        }
    }

    private ScopePop Push(Type filterMarker, bool enabled)
    {
        Stacks.Value ??= new ConcurrentDictionary<Type, Stack<bool>>();
        var stack = Stacks.Value.GetOrAdd(filterMarker, static _ => new Stack<bool>());
        lock (stack) { stack.Push(enabled); }
        return new ScopePop(stack);
    }

    private sealed class ScopePop : IDisposable
    {
        private Stack<bool>? _stack;
        public ScopePop(Stack<bool> stack) => _stack = stack;
        public void Dispose()
        {
            if (_stack is null) return;
            lock (_stack)
            {
                if (_stack.Count > 0) _stack.Pop();
            }
            _stack = null;
        }
    }
}

/// <summary>Typed adapter so callers can inject <c>IDataFilter&lt;ISoftDeletable&gt;</c>.</summary>
public sealed class DataFilter<TFilter> : IDataFilter<TFilter> where TFilter : class
{
    private readonly IDataFilter _inner;
    public DataFilter(IDataFilter inner) => _inner = inner;

    public IDisposable Disable() => _inner.Disable<TFilter>();
    public IDisposable Enable() => _inner.Enable<TFilter>();
    public bool IsEnabled => _inner.IsEnabled<TFilter>();
}
