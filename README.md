# fcg-payments-api

Microsservico de **Pagamentos**: processa (simula) o pagamento de uma compra. Consome `OrderPlacedEvent` e publica `PaymentProcessedEvent`.

## Finalidade

Quando a CatalogAPI registra um pedido, ela publica `OrderPlacedEvent`. A PaymentsAPI consome o evento, simula o pagamento, persiste o resultado na tabela `payments` (banco `fcg_payments`) e publica `PaymentProcessedEvent`, que conclui ou rejeita o pedido na CatalogAPI e dispara o e-mail de confirmacao na NotificationsAPI.

Regra de simulacao: `price <= Payments__ApprovalLimit` (padrao `300`) -> `Approved` com `reason = null`; acima disso -> `Rejected` com `reason = "Valor 399.90 excede o limite de aprovação 300.00"`. Valor negativo e rejeitado como erro de negocio (`BusinessException`, sem retry; a mensagem vai para `payments.order-placed_error`).

Idempotencia: a inbox do MassTransit descarta mensagens repetidas (mesmo `MessageId`), `order_id` e unico na tabela `payments` e, se chegar outro `OrderPlacedEvent` para um pedido ja processado, o resultado salvo e **republicado** (novo `EventId`) sem reprocessar.

## Eventos

| Direcao | Evento | Exchange | Fila | Payload |
|---|---|---|---|---|
| Consome | `OrderPlacedEvent` (`urn:message:fcg:order-placed:v1`) | `fcg.order-placed.v1` | `payments.order-placed` (variavel `Messaging__Queues__PaymentsOrderPlaced`) | [`order-placed.json`](https://github.com/sampaiobrenner/fcg-contracts/blob/main/samples/order-placed.json) |
| Publica | `PaymentProcessedEvent` (`urn:message:fcg:payment-processed:v1`) | `fcg.payment-processed.v1` | consumido por `catalog.payment-processed` e `notifications.payment-processed` | [`payment-processed.json`](https://github.com/sampaiobrenner/fcg-contracts/blob/main/samples/payment-processed.json) |

A publicacao usa o EF Core Outbox (evento gravado na mesma transacao do pagamento). No broker, enums trafegam como texto (`"status": "Approved"`) e valores como numero (`"price": 199.90`), conforme o contrato; na leitura, `price` tambem e aceito como texto (padrao do MassTransit).

## Endpoints

| Metodo | Rota | Autenticacao | Respostas |
|---|---|---|---|
| `GET` | `/api/v1/payments/{orderId}` | Bearer JWT (`User` ou `Administrator`) | `200` pagamento; `401` sem token; `404` inexistente ou de outro usuario |
| `GET` | `/health/live`, `/health/ready` | anonimo | `200` / `503` |

Somente o dono do pedido (`sub == userId`) ou um `Administrator` ve o pagamento; para os demais a resposta e `404` (nao revela a existencia).

```json
{
  "paymentId": "0199a1b2-0000-7000-8000-0000000000d1",
  "orderId": "0199a1b2-0000-7000-8000-0000000000c1",
  "userId": "0199a1b2-0000-7000-8000-0000000000a1",
  "gameId": "0199a1b2-0000-7000-8000-0000000000b1",
  "amount": 399.90,
  "status": "Rejected",
  "reason": "Valor 399.90 excede o limite de aprovação 300.00",
  "processedAt": "2026-10-01T12:05:02.123Z"
}
```

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
- **Consumers:** ficam na WebApi, delegam para um `Command` via MediatR e sao idempotentes (inbox do MassTransit). Cada consumer tem um `ConsumerDefinition` com `EndpointName` lido de `Messaging:Queues:<Fila>` (ConfigMap) e, na ausencia, a constante de `Fcg.Contracts.Messaging.QueueNames`.
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

Para testar sem a UsersAPI, gere um JWT HS256 com a chave de desenvolvimento (`Jwt__Key` do `appsettings.Development.json`), `iss=fcg-users-api`, `aud=fcg` e as claims `sub`, `email`, `name` e `role` (ver `test/Fcg.Payments.IntegrationTests/_Fixtures/TestJwtFactory.cs`).

Para simular um pedido, publique pela UI do RabbitMQ (http://localhost:15672) na exchange `fcg.order-placed.v1` com o envelope do MassTransit:

```json
{
  "messageId": "0199a1b2-0000-7000-8000-0000000000f1",
  "messageType": ["urn:message:fcg:order-placed:v1"],
  "message": {
    "eventId": "0199a1b2-0000-7000-8000-000000000002",
    "occurredAt": "2026-10-01T12:05:00Z",
    "orderId": "0199a1b2-0000-7000-8000-0000000000c1",
    "userId": "0199a1b2-0000-7000-8000-0000000000a1",
    "gameId": "0199a1b2-0000-7000-8000-0000000000b1",
    "price": 199.90
  }
}
```

(propriedade `content_type` = `application/vnd.masstransit+json`). Em seguida, `GET /api/v1/payments/0199a1b2-0000-7000-8000-0000000000c1` com o token do usuario `...a1` ou de um administrador.

Migrations (as pendentes sao aplicadas no startup quando `Database__ApplyMigrationsOnStartup=true`):

```bash
dotnet tool restore
dotnet ef migrations add <Nome> -p src/Fcg.Payments.Infrastructure -s src/Fcg.Payments.WebApi -o _Shared/Context/Migrations
```

## Testes

```bash
dotnet test
```

- `Fcg.Payments.UnitTests`: dominio (fronteira 300.00 aprova, 300.01 rejeita, valor negativo), handler (Moq), options, consumer/definition e conversores.
- `Fcg.Payments.IntegrationTests`: sobe um PostgreSQL com **Testcontainers** (requer Docker), aplica as migrations e usa o `ITestHarness` do MassTransit com o EF Outbox/Inbox real: Approved, Rejected, pedido duplicado, payload comparado com `samples/payment-processed.json`, nome da fila, limite sobrescrito por configuracao e regras de acesso do `GET`.

## CI/CD

Workflow unico [`ci.yml`](.github/workflows/ci.yml):

| Job | Quando | O que faz |
|---|---|---|
| `build-test` | PR, push na `main`, tags `v*` | restore, build, testes com cobertura e analise no SonarCloud (projeto `sampaiobrenner_fcg-payments-api`, requer o secret `SONAR_TOKEN`) |
| `docker` | PR | build da imagem |
| `publish` | push na `main` e tags `v*`, apos os testes | publica `ghcr.io/sampaiobrenner/fcg-payments-api` (`latest`, `sha-<commit>`, semver) usando o [`docker-publish.yml`](https://github.com/sampaiobrenner/fcg-orchestration/blob/main/.github/workflows/docker-publish.yml) do `fcg-orchestration` |

## Docker

```bash
docker build -t ghcr.io/sampaiobrenner/fcg-payments-api:latest .
docker run --rm -p 5103:8080 \
  -e ConnectionStrings__Default="Host=host.docker.internal;Port=5432;Database=fcg_payments;Username=fcg;Password=fcg" \
  -e RabbitMq__Host=host.docker.internal -e RabbitMq__Username=fcg -e RabbitMq__Password=fcg \
  -e Jwt__Key=fcg-dev-only-signing-key-change-me-0123456789 \
  ghcr.io/sampaiobrenner/fcg-payments-api:latest
```

Imagem multi-stage (`sdk:10.0` -> `aspnet:10.0`), usuario non-root (`$APP_UID`), porta `8080`. O `.dockerignore` exclui `bin/`, `obj/`, `test/` e `.git/`.

## Kubernetes

Manifests em [`k8s/`](k8s):

| Arquivo | Conteudo |
|---|---|
| `deployment.yaml` | `Deployment` `payments-api`, 2 replicas, `envFrom` ConfigMap + Secret, `startupProbe`/`livenessProbe` em `/health/live`, `readinessProbe` em `/health/ready`, requests `100m/128Mi`, limits `500m/256Mi` |
| `service.yaml` | `Service` `ClusterIP` `payments-api:80` -> `http` (8080) |
| `configmap.yaml` | `payments-api-config`: configuracoes nao sensiveis, incluindo `Payments__ApprovalLimit` e `Messaging__Queues__PaymentsOrderPlaced` |
| `secret.yaml` | `payments-api-secret`: connection string, credenciais do RabbitMQ e chave JWT (valores de desenvolvimento) |

```bash
docker build -t ghcr.io/sampaiobrenner/fcg-payments-api:latest .
kind load docker-image ghcr.io/sampaiobrenner/fcg-payments-api:latest   # ou: minikube image load
kubectl apply -f k8s/
kubectl get pods -l app=payments-api
kubectl port-forward svc/payments-api 8080:80 && curl localhost:8080/health/ready
```

O `/health/ready` depende do PostgreSQL e do RabbitMQ do cluster (ver [`fcg-orchestration`](https://github.com/sampaiobrenner/fcg-orchestration)). As migrations rodam no startup (`Database__ApplyMigrationsOnStartup=true`); o EF Core usa lock de migracao, entao as 2 replicas podem subir juntas.

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
| `Payments__ApprovalLimit` | ConfigMap | Valor maximo aprovado pela simulacao de pagamento (padrao `300`) | `300` |
| `Messaging__Queues__PaymentsOrderPlaced` | ConfigMap | Fila que consome `OrderPlacedEvent` (padrao `QueueNames.PaymentsOrderPlaced`) | `payments.order-placed` |
