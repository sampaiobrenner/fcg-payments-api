using Fcg.Payments.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fcg.Payments.Infrastructure.Payments;

internal sealed class PaymentModelConfiguration : IEntityTypeConfiguration<PaymentModel>
{
    public void Configure(EntityTypeBuilder<PaymentModel> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(payment => payment.Id);
        builder.Property(payment => payment.Id).ValueGeneratedNever();

        builder.Property(payment => payment.OrderId).IsRequired();
        builder.HasIndex(payment => payment.OrderId).IsUnique();

        builder.Property(payment => payment.UserId).IsRequired();
        builder.Property(payment => payment.GameId).IsRequired();
        builder.Property(payment => payment.Amount).HasPrecision(10, 2).IsRequired();
        builder.Property(payment => payment.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(payment => payment.Reason).HasMaxLength(300);
        builder.Property(payment => payment.ProcessedAt).IsRequired();
    }
}
