# Casos de uso e validacoes

**Status:** aceita  
**Dependencias:** [01-requisitos-e-cenarios.md](01-requisitos-e-cenarios.md), [02-modelo-de-dominio.md](02-modelo-de-dominio.md)

## Reservar produto

Entrada: `ProductId`, `CustomerId`, `Quantity` e horario UTC fornecido por uma abstracao de relogio.

Fluxo:

1. Validar header e quantidade positiva.
2. Localizar cliente e produto; retornar nao encontrado se um deles nao existir.
3. Adquirir exclusao por produto, expirar reservas vencidas desse produto e recalcular o saldo dentro da mesma secao critica.
4. Rejeitar reserva ativa ja existente para o mesmo cliente-produto.
5. Rejeitar quantidade acima do saldo; caso contrario, criar a reserva ativa com vencimento em 72 horas.
6. Persistir e devolver a representacao da reserva criada.

## Cancelar reserva

Entrada: `ProductId`, `CustomerId` vindo de `X-Customer-Id` e horario UTC.

O caso de uso expira primeiro as reservas vencidas do produto. Depois cancela a reserva ativa do par cliente-produto, se existir. Ausencia de reserva ativa e sucesso idempotente; cliente ou produto inexistente continua sendo recurso nao encontrado.

## Listar produtos

Antes de montar a lista, o caso de uso expira as reservas vencidas relevantes. Cada item retorna a quantidade total, a quantidade reservada, o saldo e o status derivado.

## Listar reservas de cliente

O caso de uso expira primeiro as reservas vencidas do cliente e retorna suas reservas, inclusive as terminalmente canceladas ou expiradas, para tornar a exclusao logica auditavel. A resposta identifica claramente o estado de cada item.

## Erros de negocio

- Quantidade ausente, zero ou negativa: validacao invalida.
- Header de cliente ausente ou invalido: validacao invalida.
- Cliente ou produto ausente: nao encontrado.
- Reserva ativa ja existente no mesmo cliente-produto: conflito.
- Saldo insuficiente: conflito por produto indisponivel.
