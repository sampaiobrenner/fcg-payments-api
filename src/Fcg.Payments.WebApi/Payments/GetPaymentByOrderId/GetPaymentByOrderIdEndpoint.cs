using Fcg.Payments.Application.Payments.GetPaymentByOrderId;
using Fcg.Payments.WebApi._Shared.Endpoints;
using MediatR;

namespace Fcg.Payments.WebApi.Payments.GetPaymentByOrderId;

internal sealed class GetPaymentByOrderIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/payments/{orderId:guid}", async (Guid orderId, ISender sender, CancellationToken cancellationToken) =>
                TypedResults.Ok(await sender.Send(new GetPaymentByOrderIdQuery(orderId), cancellationToken)))
            .RequireAuthorization()
            .WithName("GetPaymentByOrderId")
            .WithTags("Payments")
            .Produces<GetPaymentByOrderIdResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
