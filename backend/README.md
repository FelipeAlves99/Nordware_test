# Product Reservation API

API .NET 10 para reserva temporaria de produtos. Consulte [../docs/CONTEXT.md](../docs/CONTEXT.md) e as [specs](../docs/specs/00-processo-e-indice.md) para os requisitos, convencoes e estado do trabalho.

## Estado

Os casos de uso, persistencia InMemory, seed, protecao de concorrencia por produto, worker de expiracao e endpoints HTTP estao implementados.

## Executar

Na pasta `backend/`:

```powershell
dotnet restore ProductReservation.slnx
dotnet build ProductReservation.slnx --no-restore
dotnet test ProductReservation.slnx --no-restore
dotnet run --project src/ProductReservation.Api
```

Na inicializacao, a API cria o banco em memoria e aplica o seed. O processo reinicia o banco quando a aplicacao para.

## Endpoints

| Metodo | Rota | Resultado |
| --- | --- | --- |
| `GET` | `/products` | Produtos, quantidades total/reservada/disponivel e status (`Available`, `Reserved` ou `Unavailable`). |
| `POST` | `/products/{id}/reserve` | Cria uma reserva de quantidade informada. Exige `X-Customer-Id`; retorna `201`. |
| `DELETE` | `/products/{id}/reserve` | Cancela a reserva ativa do cliente indicado por `X-Customer-Id`; retorna `204`, inclusive se nao houver reserva ativa. |
| `GET` | `/customer/{id_customer}/reservations` | Reservas do cliente e estado persistido. |

Exemplo de reserva:

```powershell
Invoke-RestMethod -Method Post `
  -Uri http://localhost:5000/products/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/reserve `
  -Headers @{ 'X-Customer-Id' = '11111111-1111-1111-1111-111111111111' } `
  -ContentType 'application/json' `
  -Body '{"quantity":3}'
```

Erros seguem Problem Details (`application/problem+json`) e incluem um `code` estavel, por exemplo `CustomerIdRequired`, `ProductNotFound`, `ProductUnavailable` ou `ValidationError`. A identidade em `X-Customer-Id` e apenas declarada pelo chamador: nao ha autenticacao.

## Seed inicial

Os identificadores estaveis usados pelo seed e pelos exemplos futuros sao:

| Tipo | Nome | Id | Dados |
| --- | --- | --- | --- |
| Cliente | Cliente A | `11111111-1111-1111-1111-111111111111` | — |
| Cliente | Cliente B | `22222222-2222-2222-2222-222222222222` | — |
| Produto | Produto A | `aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa` | 10 unidades |
| Produto | Produto B | `bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb` | 1 unidade |
| Produto | Produto C | `cccccccc-cccc-cccc-cccc-cccccccccccc` | 0 unidades |

## Persistencia e concorrencia

O `AppDbContext` usa EF Core InMemory. O estoque e protegido por um lock em memoria por `ProductId`: operacoes sobre o mesmo produto sao serializadas, enquanto produtos diferentes podem prosseguir em paralelo. Essa garantia cobre apenas uma instancia do processo; nao representa um lock distribuido nem suporta multiplas replicas. Reservas concorrentes que excedam o saldo recebem `409 Conflict`.

## Expiracao de reservas

Os casos de uso deixam de contar reservas quando `ExpiresAtUtc` e atingido. Um job Quartz dispara junto a API e, depois, a cada hora; cada execucao processa ate 100 reservas vencidas. O job usa o mesmo lock por produto dos casos de uso; se falhar, Quartz tenta novamente apos 1, 5 e 15 minutos. Durante encerramento gracioso, a API solicita o cancelamento do job e aguarda sua conclusao. Ate o processamento, o estado armazenado pode continuar `Active`, embora a reserva nao consuma saldo. O exemplo `ExecuteUpdateAsync` no processador esta comentado para referencia futura e nao e usado com o provider InMemory.

## Estrutura

```text
src/ProductReservation.Domain           # modelo e regras de negocio
src/ProductReservation.Application      # casos de uso e abstracoes
src/ProductReservation.Infrastructure   # EF Core InMemory
src/ProductReservation.Api              # Minimal API e composicao
tests/                                  # testes por camada
```
