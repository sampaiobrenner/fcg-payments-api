using Fcg.Contracts.Security;
using Fcg.Payments.Application._Shared.Security;

namespace Fcg.Payments.WebApi._Shared.Security;

internal sealed class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public Guid? UserId => Guid.TryParse(FindClaim(FcgClaimTypes.UserId), out var userId) ? userId : null;

    public string? Email => FindClaim(FcgClaimTypes.Email);

    public string? Name => FindClaim(FcgClaimTypes.Name);

    public bool IsAdministrator => _httpContextAccessor.HttpContext?.User.IsInRole(FcgRoles.Administrator) == true;

    private string? FindClaim(string claimType) => _httpContextAccessor.HttpContext?.User.FindFirst(claimType)?.Value;
}
