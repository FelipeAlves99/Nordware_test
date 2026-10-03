# Product Reservation API

API .NET 10 para reserva temporaria de produtos. Consulte [../docs/CONTEXT.md](../docs/CONTEXT.md) e as [specs](../docs/specs/00-processo-e-indice.md) para os requisitos, convencoes e estado do trabalho.

## Estado

Os casos de uso, persistencia InMemory, seed e protecao de concorrencia por produto estao implementados. Os endpoints HTTP ainda serao adicionados em uma etapa posterior.

## Executar

Na pasta `backend/`:

```powershell
dotnet restore ProductReservation.slnx
dotnet build ProductReservation.slnx --no-restore
dotnet test ProductReservation.slnx --no-restore
dotnet run --project src/ProductReservation.Api
```

Na inicializacao, a API cria o banco em memoria e aplica o seed. O processo reinicia o banco quando a aplicacao para; os endpoints de negocio ainda nao foram registrados.

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

O `AppDbContext` usa EF Core InMemory. O estoque e protegido por um lock em memoria por `ProductId`: operacoes sobre o mesmo produto sao serializadas, enquanto produtos diferentes podem prosseguir em paralelo. Essa garantia cobre apenas uma instancia do processo; nao representa um lock distribuido nem suporta multiplas replicas.

## Expiracao de reservas

Os casos de uso deixam de contar reservas quando `ExpiresAtUtc` e atingido. Um worker futuro persistira o estado `Expired`; ate ele processar a reserva, o estado armazenado pode continuar `Active`. A implementacao do worker ficou para a etapa final e ainda nao esta incluida.

## Estrutura

```text
src/ProductReservation.Domain           # modelo e regras de negocio
src/ProductReservation.Application      # casos de uso e abstracoes
src/ProductReservation.Infrastructure   # EF Core InMemory
src/ProductReservation.Api              # Minimal API e composicao
tests/                                  # testes por camada
```
