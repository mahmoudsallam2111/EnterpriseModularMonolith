namespace Customers.Application.Dtos;

public sealed record CustomerDetailsDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Status,
    DateTimeOffset CreatedAt);
