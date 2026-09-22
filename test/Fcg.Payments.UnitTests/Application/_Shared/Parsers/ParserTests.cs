using Fcg.Payments.Application._Shared.Parsers;

namespace Fcg.Payments.UnitTests.Application._Shared.Parsers;

public class ParserTests
{
    private sealed class LengthParser : Parser<string, int>
    {
        protected override int DoParse(string source) => source.Length;
    }

    private readonly LengthParser _parser = new();

    [Fact]
    public void Parse_DeveRetornarDefault_QuandoOrigemNula()
    {
        _parser.Parse((string?)null).Should().Be(default);
    }

    [Fact]
    public void Parse_DeveConverterItem_QuandoOrigemInformada()
    {
        _parser.Parse("fcg").Should().Be(3);
    }

    [Fact]
    public void Parse_DeveRetornarColecaoVazia_QuandoColecaoNula()
    {
        _parser.Parse((IEnumerable<string>?)null).Should().BeEmpty();
    }

    [Fact]
    public void Parse_DeveConverterTodosOsItens_QuandoColecaoInformada()
    {
        _parser.Parse(["a", "bb", "ccc"]).Should().Equal(1, 2, 3);
    }
}
