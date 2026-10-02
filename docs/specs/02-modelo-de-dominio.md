# Modelo de dominio

**Status:** implementada  
**Dependencia:** [01-requisitos-e-cenarios.md](01-requisitos-e-cenarios.md)

## Entidades

### Customer

- `Id`: GUID gerado no construtor.
- `Name`: propriedade `required`, atribuida pelo construtor.

O cliente existe para ser dono logico de uma reserva. A ausencia de autenticacao nao remove essa relacao; apenas torna a identidade declarada pelo header nao confiavel.

### Product

- `Id`: GUID gerado no construtor.
- `Name` e `TotalQuantity`: propriedades `required`, atribuidas pelo construtor.
- `TotalQuantity`: estoque total, inteiro nao negativo.
- O saldo e derivado das reservas ativas; nao e um valor independente a ser atualizado por varios pontos do codigo.

O produto expoe comportamento de dominio para verificar se uma quantidade cabe no saldo e para derivar seu status. O status nao deve ser salvo como dado mutavel independente.

`ProductStatus` e uma entidade pequena, identificada somente por `Id`, cujo valor e o nome estavel do status (`Available`, `Reserved` ou `Unavailable`). A disponibilidade do produto aponta para uma dessas instancias; na persistencia EF Core, `ProductStatus` sera uma entidade de lookup com chave primaria textual.

### Reservation

- `Id`: GUID gerado no construtor.
- `CustomerId`, `ProductId`, `Quantity` e `CreatedAtUtc`: propriedades `required`, atribuidas pelo construtor. `CreatedAtUtc` e gerado pelo relogio UTC do servidor; a API nao recebe essa data.
- `ExpiresAtUtc`: derivado automaticamente como 72 horas apos `CreatedAtUtc`.
- `Status`: `Active`, `Cancelled` ou `Expired`.
- `CancelledAtUtc` ou `ExpiredAtUtc`, quando correspondente.

`ReservationStatus` e uma entidade pequena identificada somente por `Id`, com os valores estaveis `Active`, `Cancelled` e `Expired`. Uma reserva somente consome saldo em `Active` e enquanto `ExpiresAtUtc` for posterior ao relogio atual. Cancelar e expirar sao transicoes terminais; uma reserva terminal jamais volta a ativa. O dominio nao valida o offset UTC dos timestamps.

## Invariantes

- Quantidades de produto e reserva sao inteiros; reserva requer valor maior que zero.
- A soma de reservas ativas nunca ultrapassa `TotalQuantity`.
- Ha no maximo uma reserva ativa para o mesmo `CustomerId` e `ProductId`.
- Horarios sao armazenados em UTC e a duracao e exatamente 72 horas.
- `disponivel`: reservado igual a zero; `reservado`: reservado maior que zero e menor que o total; `indisponivel`: reservado igual ao total. Um produto com total zero e `indisponivel`.

## Responsabilidades por camada

- O **Domain** conhece entidades, estados, invariantes e transicoes.
- A **Application** consulta reservas, aplica a expiracao antes de decidir e coordena o caso de uso.
- A **Infrastructure** persiste o modelo e fornece sincronizacao/relatorio de tempo pelas abstracoes necessarias.
- A **API** converte HTTP para comandos e resultados; nao recalcula saldo nem muda estados diretamente.
