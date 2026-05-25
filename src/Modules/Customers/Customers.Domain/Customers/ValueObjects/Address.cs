using BuildingBlocks.Domain;

namespace Customers.Domain.Customers.ValueObjects;

public sealed class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string PostalCode { get; }
    public string Country { get; }

    private Address(string street, string city, string postalCode, string country)
    {
        Street = street; City = city; PostalCode = postalCode; Country = country;
    }

    public static Address Create(string street, string city, string postalCode, string country)
    {
        if (string.IsNullOrWhiteSpace(street)) throw new ArgumentException("Street required.", nameof(street));
        if (string.IsNullOrWhiteSpace(city)) throw new ArgumentException("City required.", nameof(city));
        if (string.IsNullOrWhiteSpace(postalCode)) throw new ArgumentException("Postal code required.", nameof(postalCode));
        if (string.IsNullOrWhiteSpace(country)) throw new ArgumentException("Country required.", nameof(country));
        return new Address(street.Trim(), city.Trim(), postalCode.Trim(), country.Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return PostalCode;
        yield return Country;
    }

    public override string ToString() => $"{Street}, {PostalCode} {City}, {Country}";
}
