namespace Orders.Domain.Orders;

public enum OrderStatus
{
    Draft = 1,
    Placed = 2,
    Paid = 3,
    Shipped = 4,
    Completed = 5,
    Cancelled = 99
}
