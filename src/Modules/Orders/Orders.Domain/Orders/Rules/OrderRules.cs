using BuildingBlocks.Domain;

namespace Orders.Domain.Orders.Rules;

public sealed class OrderMustBeInDraftToModify : IBusinessRule
{
    private readonly OrderStatus _status;
    public OrderMustBeInDraftToModify(OrderStatus status) => _status = status;
    public string Code => "Orders.MustBeDraftToModify";
    public string Message => $"Order is {_status}; only draft orders can be modified.";
    public bool IsBroken() => _status != OrderStatus.Draft;
}

public sealed class OrderMustHaveAtLeastOneLineToPlace : IBusinessRule
{
    private readonly int _lineCount;
    public OrderMustHaveAtLeastOneLineToPlace(int lineCount) => _lineCount = lineCount;
    public string Code => "Orders.MustHaveLinesToPlace";
    public string Message => "Order must have at least one line to be placed.";
    public bool IsBroken() => _lineCount == 0;
}

public sealed class OrderCannotBeCancelledOnceCompleted : IBusinessRule
{
    private readonly OrderStatus _status;
    public OrderCannotBeCancelledOnceCompleted(OrderStatus status) => _status = status;
    public string Code => "Orders.CannotCancelCompleted";
    public string Message => "Order is already completed and cannot be cancelled.";
    public bool IsBroken() => _status == OrderStatus.Completed;
}
