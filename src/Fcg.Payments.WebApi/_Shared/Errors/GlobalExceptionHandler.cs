using Fcg.Payments.Application._Shared.Exceptions;
using Fcg.Payments.Domain._Shared.Exceptions;
using Fcg.Payments.WebApi.Properties;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Fcg.Payments.WebApi._Shared.Errors;

internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, WebApiResources.RequisicaoInvalida),
            NotFoundException => (StatusCodes.Status404NotFound, WebApiResources.RecursoNaoEncontrado),
            BusinessException => (StatusCodes.Status422UnprocessableEntity, WebApiResources.RegraDeNegocioViolada),
            _ => (StatusCodes.Status500InternalServerError, WebApiResources.ErroInesperado)
        };

        if (status == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Erro nao tratado em {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = status == StatusCodes.Status500InternalServerError ? null : exception.Message
        };

        if (exception is ValidationException validationException)
        {
            problemDetails.Extensions["errors"] = validationException.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());
        }

        httpContext.Response.StatusCode = status;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });
    }
}
