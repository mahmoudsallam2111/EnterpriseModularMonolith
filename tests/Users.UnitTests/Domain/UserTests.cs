using BuildingBlocks.Domain;
using FluentAssertions;
using Users.Domain.Users;
using Users.Domain.Users.Events;
using Xunit;

namespace Users.UnitTests.Domain;

public class UserTests
{
    [Fact]
    public void Register_should_raise_registered_event()
    {
        var user = User.Register(
            "ada",
            UserEmail.Create("ada@example.com"),
            PasswordHash.FromHash("$2a$12$abcdefghijklmnopqrstu0123456789"));

        user.DomainEvents.Should().Contain(e => e is UserRegisteredDomainEvent);
    }

    [Fact]
    public void Five_failed_logins_should_lock_user_out()
    {
        var user = User.Register("ada", UserEmail.Create("ada@example.com"),
            PasswordHash.FromHash("$2a$12$abcdefghijklmnopqrstu0123456789"));

        for (var i = 0; i < 5; i++)
            user.RecordFailedLogin(DateTimeOffset.UtcNow);

        user.IsLockedOut.Should().BeTrue();
        user.DomainEvents.Should().Contain(e => e is UserLockedOutDomainEvent);
    }

    [Fact]
    public void Successful_login_should_reset_failed_attempts()
    {
        var user = User.Register("ada", UserEmail.Create("ada@example.com"),
            PasswordHash.FromHash("$2a$12$abcdefghijklmnopqrstu0123456789"));

        user.RecordFailedLogin(DateTimeOffset.UtcNow);
        user.RecordFailedLogin(DateTimeOffset.UtcNow);
        user.RecordSuccessfulLogin(DateTimeOffset.UtcNow);

        user.FailedLoginAttempts.Should().Be(0);
    }

    [Fact]
    public void Cannot_assign_role_to_locked_user()
    {
        var user = User.Register("ada", UserEmail.Create("ada@example.com"),
            PasswordHash.FromHash("$2a$12$abcdefghijklmnopqrstu0123456789"));
        user.LockOut("manual lock");

        var act = () => user.AssignRole(RoleId.New());
        act.Should().Throw<BusinessRuleValidationException>();
    }
}
