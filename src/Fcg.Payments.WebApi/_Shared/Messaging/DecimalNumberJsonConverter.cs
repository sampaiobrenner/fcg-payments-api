using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fcg.Payments.WebApi._Shared.Messaging;

/// <summary>
/// Escreve <see cref="decimal"/> como numero JSON (schema <c>Money</c> do contrato: <c>"price": 199.90</c>) e le
/// tanto numero quanto texto, para aceitar publicadores que usam o padrao do MassTransit (<c>"price": "199.90"</c>).
/// </summary>
public sealed class DecimalNumberJsonConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType == JsonTokenType.String
            ? decimal.Parse(reader.GetString()!, NumberStyles.Number, CultureInfo.InvariantCulture)
            : reader.GetDecimal();

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}
