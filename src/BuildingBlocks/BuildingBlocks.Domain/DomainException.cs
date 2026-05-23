namespace BuildingBlocks.Domain;

/// <summary>
/// Root of the domain exception hierarchy. Catch this in the presentation layer
/// to translate domain failures into ProblemDetails responses.
/// </summary>
public abstract class DomainException : Exception
{
    public string Code { get; }

    protected DomainException(string code, string message) : base(message)
    {
        Code = code;
    }

    protected DomainException(string code, string message, Exception inner) : base(message, inner)
    {
        Code = code;
    }
}

public sealed class NotFoundDomainException : DomainException
{
    public NotFoundDomainException(string code, string message) : base(code, message) { }
}

public sealed class ConflictDomainException : DomainException
{
    public ConflictDomainException(string code, string message) : base(code, message) { }
}
