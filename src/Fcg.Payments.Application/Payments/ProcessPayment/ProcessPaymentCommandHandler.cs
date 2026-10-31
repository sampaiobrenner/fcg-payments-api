using Fcg.Contracts.Events.V1;
using Fcg.Payments.Application._Shared.Contexts;
using Fcg.Payments.Application._Shared.Messaging;
using Fcg.Payments.Application._Shared.Parsers;
using Fcg.Payments.Application.Payments._Shared;
using Fcg.Payments.Domain.Payments;
using MediatR;
using Microsoft.Extensions.Options;

namespace Fcg.Payments.Application.Payments.ProcessPayment;

internal sealed class ProcessPaymentCommandHandler : IRequestHandler<ProcessPaymentCommand>
{
    private readonly IPaymentRepository _repository;
    private readonly IPaymentsDbContext _context;
    private readonly IIntegrationEventPublisher _publisher;
    private readonly IParser<Payment, PaymentProcessedEvent> _parser;
    private readonly PaymentsOptions _options;

    public ProcessPaymentCommandHandler(
        IPaymentRepository repository,
        IPaymentsDbContext context,
        IIntegrationEventPublisher publisher,
        IParser<Payment, PaymentProcessedEvent> parser,
        IOptions<PaymentsOptions> options)
    {
        _repository = repository;
        _context = context;
        _publisher = publisher;
        _parser = parser;
        _options = options.Value;
    }

    public async Task Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        // Idempotencia de negocio: pedido ja processado -> republica o resultado salvo (novo EventId), sem reprocessar.
        var existing = await _repository.GetByOrderIdAsync(request.OrderId, cancellationToken);
        if (existing is not null)
        {
            await _publisher.PublishAsync(_parser.Parse(existing)!, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        var payment = Payment.Process(request.OrderId, request.UserId, request.GameId, request.Price, _options.ApprovalLimit);

        await _repository.AddAsync(payment, cancellationToken);

        // Publicado antes do SaveChanges: o outbox grava o evento na mesma transacao do pagamento.
        await _publisher.PublishAsync(_parser.Parse(payment)!, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
