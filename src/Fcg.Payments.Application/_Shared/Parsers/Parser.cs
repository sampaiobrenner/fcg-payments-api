namespace Fcg.Payments.Application._Shared.Parsers;

public abstract class Parser<TInput, TOutput> : IParser<TInput, TOutput>
{
    public TOutput? Parse(TInput? source)
        => source is null ? default : DoParse(source);

    public IReadOnlyCollection<TOutput> Parse(IEnumerable<TInput>? source)
        => source is null ? [] : source.Where(item => item is not null).Select(DoParse).ToList();

    protected abstract TOutput DoParse(TInput source);
}
