using Fcg.Payments.Application._Shared.Exceptions;
using Fcg.Payments.Domain._Shared.Exceptions;
using Fcg.Payments.WebApi._Shared.Errors;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fcg.Payments.UnitTests.WebApi._Shared.Errors;

public class GlobalExceptionHandlerTests
{
    private readonly Mock<IProblemDetailsService> _problemDetailsService = new();
    private readonly GlobalExceptionHandler _handler;

    public GlobalExceptionHandlerTests()
    {
        _problemDetailsService
            .Setup(service => service.TryWriteAsync(It.IsAny<ProblemDetailsContext>()))
            .ReturnsAsync(true);

        _handler = new GlobalExceptionHandler(_problemDetailsService.Object, NullLogger<GlobalExceptionHandler>.Instance);
    }

    public static TheoryData<Exception, int> Excecoes => new()
    {
        { new ValidationException([new ValidationFailure("Nome", "obrigatorio")]), StatusCodes.Status400BadRequest },
        { new UnauthorizedException("credenciais"), StatusCodes.Status401Unauthorized },
        { new ForbiddenException("acesso"), StatusCodes.Status403Forbidden },
        { new NotFoundException("Jogo", Guid.Empty), StatusCodes.Status404NotFound },
        { new ConflictException("duplicado"), StatusCodes.Status409Conflict },
        { new BusinessException("regra"), StatusCodes.Status422UnprocessableEntity },
        { new InvalidOperationException("falha"), StatusCodes.Status500InternalServerError }
    };

    [Theory]
    [MemberData(nameof(Excecoes))]
    public async Task TryHandleAsync_DeveDefinirStatusCode_QuandoExcecaoConhecida(Exception exception, int expectedStatus)
    {
        var httpContext = new DefaultHttpContext();

        var handled = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        handled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(expectedStatus);
        _problemDetailsService.Verify(service => service.TryWriteAsync(
            It.Is<ProblemDetailsContext>(context => context.ProblemDetails.Status == expectedStatus)), Times.Once);
    }

    [Fact]
    public async Task TryHandleAsync_DeveOcultarDetalhe_QuandoErroInesperado()
    {
        await _handler.TryHandleAsync(new DefaultHttpContext(), new InvalidOperationException("segredo"), CancellationToken.None);

        _problemDetailsService.Verify(service => service.TryWriteAsync(
            It.Is<ProblemDetailsContext>(context => context.ProblemDetails.Detail == null)), Times.Once);
    }
}
