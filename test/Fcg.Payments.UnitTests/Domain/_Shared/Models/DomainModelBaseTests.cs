using Fcg.Payments.Domain._Shared.Events;
using Fcg.Payments.Domain._Shared.Models;

namespace Fcg.Payments.UnitTests.Domain._Shared.Models;

public class DomainModelBaseTests
{
    private sealed class SampleModel : PersistenceModelBase;

    private sealed record SampleEvent(Guid Id) : IDomainEvent;

    private sealed class Sample : DomainModelBase<SampleModel>
    {
        public Sample(SampleModel persistenceModel) : base(persistenceModel)
        {
        }

        public void Raise() => AddDomainEvent(new SampleEvent(Id()));
    }

    [Fact]
    public void Construtor_DeveLancarExcecao_QuandoModeloDePersistenciaNulo()
    {
        var act = () => new Sample(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Id_DeveRetornarIdDoModeloDePersistencia_QuandoCriado()
    {
        var model = new SampleModel();

        new Sample(model).Id().Should().Be(model.Id);
    }

    [Fact]
    public void AddDomainEvent_DeveRegistrarEvento_QuandoComportamentoExecutado()
    {
        var sample = new Sample(new SampleModel());

        sample.Raise();

        sample.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<SampleEvent>();
    }

    [Fact]
    public void ClearDomainEvents_DeveRemoverEventos_QuandoChamado()
    {
        var sample = new Sample(new SampleModel());
        sample.Raise();

        sample.ClearDomainEvents();

        sample.DomainEvents.Should().BeEmpty();
    }
}
