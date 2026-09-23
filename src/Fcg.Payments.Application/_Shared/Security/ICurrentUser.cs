namespace Fcg.Payments.Application._Shared.Security;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }

    string? Email { get; }

    string? Name { get; }

    bool IsAdministrator { get; }
}
