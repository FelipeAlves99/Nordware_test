# Product Reservation API

Esqueleto inicial de uma API .NET 10 para reserva temporaria de produtos. Consulte [../docs/CONTEXT.md](../docs/CONTEXT.md) para os requisitos, convencoes e estado do trabalho.

## Estado

Ainda nao ha funcionalidades implementadas. A solucao contem somente a separacao de camadas, projetos e referencias necessarias para iniciar o desenvolvimento.

## Verificar o esqueleto

Na pasta `backend/`:

```powershell
dotnet restore ProductReservation.slnx
dotnet build ProductReservation.slnx --no-restore
dotnet run --project src/ProductReservation.Api
```

A aplicacao iniciara sem endpoints de negocio ate a proxima etapa de desenvolvimento.

## Expiracao de reservas

A estrategia de expiracao sera definida na primeira etapa de implementacao. Ela devera garantir que um produto volte automaticamente a `disponivel` 72 horas apos a reserva e ser documentada nesta secao junto com os testes correspondentes.

## Estrutura

```text
src/ProductReservation.Domain           # modelo e regras de negocio
src/ProductReservation.Application      # casos de uso e abstracoes
src/ProductReservation.Infrastructure   # EF Core InMemory
src/ProductReservation.Api              # Minimal API e composicao
tests/                                  # testes por camada
```
