using Fcg.Payments.Application.Properties;

namespace Fcg.Payments.Application._Shared.Exceptions;

public sealed class NotFoundException : Exception
{
    public NotFoundException(string resource, object key)
        : base(string.Format(ApplicationResources.RecursoNaoEncontrado, resource, key))
    {
    }
}
