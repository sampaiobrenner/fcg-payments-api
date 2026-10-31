using System.Text.Json;
using Fcg.Payments.WebApi._Shared.Messaging;

namespace Fcg.Payments.UnitTests.WebApi._Shared.Messaging;

public class DecimalNumberJsonConverterTests
{
    private sealed record Valor(decimal Price);

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new DecimalNumberJsonConverter() }
    };

    [Fact]
    public void Write_DeveEscreverNumeroComAsCasasDecimais_QuandoSerializado()
    {
        JsonSerializer.Serialize(new Valor(199.90m), Options).Should().Be("""{"price":199.90}""");
    }

    [Theory]
    [InlineData("""{"price":199.90}""")]
    [InlineData("""{"price":"199.90"}""")]
    public void Read_DeveAceitarNumeroOuTexto_QuandoDesserializado(string json)
    {
        JsonSerializer.Deserialize<Valor>(json, Options)!.Price.Should().Be(199.90m);
    }
}
