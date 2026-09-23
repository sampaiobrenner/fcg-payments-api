using System.Security.Claims;
using Fcg.Contracts.Security;
using Fcg.Payments.WebApi._Shared.Security;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Fcg.Payments.UnitTests.WebApi._Shared.Security;

public class HttpContextCurrentUserTests
{
    [Fact]
    public void Propriedades_DevemLerClaimsDoContrato_QuandoUsuarioAutenticado()
    {
        var userId = Guid.CreateVersion7();
        var identity = new ClaimsIdentity(
            [
                new Claim(FcgClaimTypes.UserId, userId.ToString()),
                new Claim(FcgClaimTypes.Email, "ada@fcg.dev"),
                new Claim(FcgClaimTypes.Name, "Ada Lovelace"),
                new Claim(FcgClaimTypes.Role, FcgRoles.Administrator)
            ],
            authenticationType: "Bearer",
            nameType: FcgClaimTypes.Name,
            roleType: FcgClaimTypes.Role);

        var currentUser = Create(new ClaimsPrincipal(identity));

        currentUser.IsAuthenticated.Should().BeTrue();
        currentUser.UserId.Should().Be(userId);
        currentUser.Email.Should().Be("ada@fcg.dev");
        currentUser.Name.Should().Be("Ada Lovelace");
        currentUser.IsAdministrator.Should().BeTrue();
    }

    [Fact]
    public void Propriedades_DevemSerVazias_QuandoUsuarioAnonimo()
    {
        var currentUser = Create(new ClaimsPrincipal(new ClaimsIdentity()));

        currentUser.IsAuthenticated.Should().BeFalse();
        currentUser.UserId.Should().BeNull();
        currentUser.IsAdministrator.Should().BeFalse();
    }

    private static HttpContextCurrentUser Create(ClaimsPrincipal principal)
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(new DefaultHttpContext { User = principal });
        return new HttpContextCurrentUser(accessor.Object);
    }
}
