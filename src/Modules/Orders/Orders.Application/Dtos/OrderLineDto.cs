namespace Orders.Application.Dtos;

public sealed record OrderLineDto(Guid Id, string Sku, string Name, int Quantity, decimal UnitPrice, decimal LineTotal);