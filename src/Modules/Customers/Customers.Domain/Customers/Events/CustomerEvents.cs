using BuildingBlocks.Domain;

namespace Customers.Domain.Customers.Events;

public sealed record CustomerRegisteredDomainEvent(CustomerId CustomerId, string FullName, string Email) : DomainEvent;
public sealed record CustomerEmailChangedDomainEvent(CustomerId CustomerId, string OldEmail, string NewEmail) : DomainEvent;
public sealed record CustomerSuspendedDomainEvent(CustomerId CustomerId, string Reason) : DomainEvent;
public sealed record CustomerReactivatedDomainEvent(CustomerId CustomerId) : DomainEvent;
public sealed record CustomerDeactivatedDomainEvent(CustomerId CustomerId, string Reason) : DomainEvent;
