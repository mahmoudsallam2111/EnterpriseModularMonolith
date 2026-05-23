using BuildingBlocks.Domain;

namespace Users.Domain.Users.Events;

public sealed record UserRegisteredDomainEvent(UserId UserId, string UserName, string Email) : DomainEvent;
public sealed record UserPasswordChangedDomainEvent(UserId UserId) : DomainEvent;
public sealed record UserRoleAssignedDomainEvent(UserId UserId, RoleId RoleId) : DomainEvent;
public sealed record UserRoleRemovedDomainEvent(UserId UserId, RoleId RoleId) : DomainEvent;
public sealed record UserLockedOutDomainEvent(UserId UserId, string Reason) : DomainEvent;
