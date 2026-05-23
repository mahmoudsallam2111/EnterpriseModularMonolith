using BuildingBlocks.Domain;

namespace Users.Domain.Users.Rules;

public sealed class UserMustNotBeLockedOut : IBusinessRule
{
    private readonly bool _isLockedOut;
    public UserMustNotBeLockedOut(bool isLockedOut) => _isLockedOut = isLockedOut;
    public string Code => "Users.MustNotBeLockedOut";
    public string Message => "User account is locked out.";
    public bool IsBroken() => _isLockedOut;
}
