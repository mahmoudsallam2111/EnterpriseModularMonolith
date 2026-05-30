namespace Orders.Application.Dtos;

public sealed record OrderDetailsDto(
    Guid Id,
    Guid CustomerId,
    string Status,
    string Currency,
    decimal Total,
    DateTimeOffset? PlacedAtUtc,
    IReadOnlyList<OrderLineDto> Lines);
