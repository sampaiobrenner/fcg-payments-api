namespace Fcg.Payments.Application.Payments._Shared;

public sealed class PaymentsOptions
{
    public const string SectionName = "Payments";
    public const decimal DefaultApprovalLimit = 300m;

    /// <summary>Valor maximo aprovado pela simulacao (variavel <c>Payments__ApprovalLimit</c>).</summary>
    public decimal ApprovalLimit { get; set; } = DefaultApprovalLimit;
}
