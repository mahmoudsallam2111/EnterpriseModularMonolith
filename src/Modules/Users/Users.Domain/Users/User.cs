using BuildingBlocks.Domain;
using Users.Domain.Users.Events;
using Users.Domain.Users.Rules;

namespace Users.Domain.Users;

public sealed class User : AggregateRoot<UserId>, IAuditableEntity, ISoftDeletable
{
    private readonly HashSet<RoleId> _roleIds = [];

    public string UserName { get; private set; } = default!;
    public UserEmail Email { get; private set; } = default!;
    public PasswordHash Password { get; private set; } = default!;
    public bool IsLockedOut { get; private set; }
    public string? LockoutReason { get; private set; }
    public DateTimeOffset? LockoutUntilUtc { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTimeOffset? LastLoginAtUtc { get; private set; }

    public IReadOnlyCollection<RoleId> RoleIds => _roleIds;

    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    private User() { }

    private User(UserId id, string userName, UserEmail email, PasswordHash password) : base(id)
    {
        UserName = userName;
        Email = email;
        Password = password;
    }

    public static User Register(string userName, UserEmail email, PasswordHash password)
    {
        if (string.IsNullOrWhiteSpace(userName)) throw new ArgumentException("UserName required.", nameof(userName));
        if (userName.Length > 100) throw new ArgumentException("UserName too long.", nameof(userName));

        var user = new User(UserId.New(), userName.Trim(), email, password);
        user.RaiseDomainEvent(new UserRegisteredDomainEvent(user.Id, userName, email.Value));
        return user;
    }

    public void ChangePassword(PasswordHash newPassword)
    {
        Password = newPassword;
        RaiseDomainEvent(new UserPasswordChangedDomainEvent(Id));
    }

    public void AssignRole(RoleId roleId)
    {
        CheckRule(new UserMustNotBeLockedOut(IsLockedOut));
        if (_roleIds.Add(roleId))
            RaiseDomainEvent(new UserRoleAssignedDomainEvent(Id, roleId));
    }

    public void RemoveRole(RoleId roleId)
    {
        if (_roleIds.Remove(roleId))
            RaiseDomainEvent(new UserRoleRemovedDomainEvent(Id, roleId));
    }

    public void RecordSuccessfulLogin(DateTimeOffset whenUtc)
    {
        CheckRule(new UserMustNotBeLockedOut(IsLockedOut));
        FailedLoginAttempts = 0;
        LastLoginAtUtc = whenUtc;
    }

    public void RecordFailedLogin(DateTimeOffset whenUtc, int lockoutThreshold = 5, TimeSpan? lockoutDuration = null)
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= lockoutThreshold)
            LockOut("Too many failed login attempts.", whenUtc.Add(lockoutDuration ?? TimeSpan.FromMinutes(15)));
    }

    public void LockOut(string reason, DateTimeOffset? untilUtc = null)
    {
        IsLockedOut = true;
        LockoutReason = reason;
        LockoutUntilUtc = untilUtc;
        RaiseDomainEvent(new UserLockedOutDomainEvent(Id, reason));
    }

    public void Unlock()
    {
        IsLockedOut = false;
        LockoutReason = null;
        LockoutUntilUtc = null;
        FailedLoginAttempts = 0;
    }
}
