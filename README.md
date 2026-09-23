# fcg-payments-api

Microsservico de **Pagamentos**: processa (simula) o pagamento de uma compra. Consome `OrderPlacedEvent` e publica `PaymentProcessedEvent`.

Parte da plataforma **FIAP Cloud Games (FCG) - Fase 2**. Os contratos de integracao (eventos, JWT, convencoes) sao definidos no repositorio [`fcg-contracts`](https://github.com/sampaiobrenner/fcg-contracts) e a orquestracao (docker-compose e guia de Kubernetes) no [`fcg-orchestration`](https://github.com/sampaiobrenner/fcg-orchestration).

## Stack

.NET 10 · Minimal API · MediatR 12 · FluentValidation · EF Core 10 + PostgreSQL · MassTransit 8 + RabbitMQ (EF Core Outbox/Inbox) · JWT · Serilog · OpenAPI + Scalar · xUnit.

## Arquitetura

Camadas com dependencia estritamente para dentro: `WebApi -> Infrastructure -> Application -> Domain`.

```
src/
  Fcg.Payments.Domain/           Regras de negocio. Organizado por agregado.
    _Shared/                    Modules, DomainModelBase/PersistenceModelBase, IDomainEvent, BusinessException
  Fcg.Payments.Application/      Casos de uso (MediatR). Organizado por <Agregado>/<CasoDeUso>/.
    _Shared/                    IPaymentsDbContext (leitura), Parser<TIn,TOut>, IIntegrationEventPublisher, ValidationBehavior
  Fcg.Payments.Infrastructure/   Portas de saida: DbContext, repositorios, publicacao de eventos. Organizado por agregado.
    _Shared/                    PaymentsDbContext (+ tabelas de outbox/inbox), MassTransitIntegrationEventPublisher
  Fcg.Payments.WebApi/           Portas de entrada: endpoints REST e consumers. Organizado por <Agregado>/<CasoDeUso>/.
    _Shared/                    Endpoints (IEndpoint), erros (ProblemDetails), JWT, mensageria, health checks, migrations
test/
  Fcg.Payments.UnitTests/        Espelha src/ 1:1
  Fcg.Payments.IntegrationTests/ WebApplicationFactory + MassTransit test harness
```

Diretrizes:

- **Um caso de uso = uma pasta** com `Command`/`Query`, `Handler`, `Validator`, `Parser` e, na WebApi, o `Endpoint` (implementa `IEndpoint`) ou o `Consumer`.
- **Escrita:** handler -> servico/modelo de dominio -> repositorio do agregado (recebe e devolve o modelo de dominio).
- **Leitura:** handler projeta direto de `IPaymentsDbContext.DataSet<T>()`, sem repositorio.
- **Dominio:** `DomainModel` (comportamento) envolve o `PersistenceModel` (mapeado pelo EF via `IEntityTypeConfiguration<T>`, descoberto automaticamente).
- **Mapeamento:** `Parser<TIn,TOut>` explicito, sem AutoMapper.
- **Mensagens de erro:** sempre em `Properties/*Resources.resx` da propria camada, nunca string literal.
- **DI:** cada camada registra seus servicos no seu `FcgPayments<Camada>Module`; `Program.cs` apenas compoe os 4 modulos.
- **Eventos de integracao:** publicados via `IIntegrationEventPublisher` antes do `SaveChangesAsync` - o outbox grava na mesma transacao.
- **Consumers:** ficam na WebApi, delegam para um `Command` via MediatR e sao idempotentes (inbox do MassTransit). Cada consumer tem um `ConsumerDefinition` com `EndpointName` igual a constante de `Fcg.Contracts.Messaging.QueueNames`.
- **Contratos:** eventos, filas, claims e papeis vem do pacote `PosTech.Fiap.CloudGames.Contracts`; nunca redefina localmente.
- **Usuario autenticado:** injete `ICurrentUser` (Application/_Shared/Security), que le as claims `sub`, `email`, `name` e `role` do token.
- **Erros HTTP:** `ValidationException` 400, `UnauthorizedException` 401, `ForbiddenException` 403, `NotFoundException` 404, `ConflictException` 409, `BusinessException` 422.
- **JSON:** camelCase e enums como string.

Exemplo de caso de uso:

```
Application/Games/CreateGame/CreateGameCommand.cs
Application/Games/CreateGame/CreateGameCommandHandler.cs
Application/Games/CreateGame/CreateGameCommandValidator.cs
Application/Games/CreateGame/CreateGameResponseParser.cs
WebApi/Games/CreateGame/CreateGameEndpoint.cs
```

## Executando localmente

Pre-requisitos: .NET SDK 10 e a infraestrutura do [`fcg-orchestration`](https://github.com/sampaiobrenner/fcg-orchestration) (`docker compose up -d`).

```bash
dotnet run --project src/Fcg.Payments.WebApi
```

- API: http://localhost:5103
- Documentacao (Scalar): http://localhost:5103/scalar/v1
- Health: `/health/live` e `/health/ready`

Migrations (as pendentes sao aplicadas no startup quando `Database__ApplyMigrationsOnStartup=true`):

```bash
dotnet tool restore
dotnet ef migrations add <Nome> -p src/Fcg.Payments.Infrastructure -s src/Fcg.Payments.WebApi -o _Shared/Context/Migrations
```

## Testes

```bash
dotnet test
```

## Docker

```bash
docker build -t fcg-payments-api .
```

Imagem multi-stage (`sdk:10.0` -> `aspnet:10.0`), usuario non-root, porta `8080`.

## Kubernetes

Manifests em [`k8s/`](k8s): `deployment.yaml`, `service.yaml` (`payments-api:80`), `configmap.yaml`, `secret.yaml`.

```bash
kubectl apply -f k8s/
```

## Variaveis de ambiente

| Variavel | Origem K8s | Descricao | Exemplo |
|---|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | ConfigMap | Ambiente | `Production` |
| `ASPNETCORE_HTTP_PORTS` | Dockerfile | Porta HTTP | `8080` |
| `ConnectionStrings__Default` | Secret | Conexao PostgreSQL | `Host=postgres;Port=5432;Database=fcg_payments;Username=fcg;Password=***` |
| `Database__ApplyMigrationsOnStartup` | ConfigMap | Aplica migrations no startup | `true` |
| `RabbitMq__Host` | ConfigMap | Host do RabbitMQ | `rabbitmq` |
| `RabbitMq__VirtualHost` | ConfigMap | Virtual host | `/` |
| `RabbitMq__Username` | Secret | Usuario do RabbitMQ | `fcg` |
| `RabbitMq__Password` | Secret | Senha do RabbitMQ | `***` |
| `Jwt__Issuer` | ConfigMap | Emissor do token | `fcg-users-api` |
| `Jwt__Audience` | ConfigMap | Audiencia do token | `fcg` |
| `Jwt__Key` | Secret | Chave simetrica (min. 32 caracteres, igual em todos os servicos) | `***` |
