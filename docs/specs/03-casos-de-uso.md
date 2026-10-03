# Casos de uso e validacoes

**Status:** implementada
**Dependencias:** [01-requisitos-e-cenarios.md](01-requisitos-e-cenarios.md), [02-modelo-de-dominio.md](02-modelo-de-dominio.md)

## Reservar produto

Entrada: `ProductId`, `CustomerId` e `Quantity`. `CreatedAtUtc` e gerado pela entidade usando o relogio UTC do servidor; o endpoint nao recebe data.

Fluxo:

1. Validar header e quantidade positiva.
2. Localizar cliente e produto; retornar nao encontrado se um deles nao existir.
3. Adquirir exclusao por produto e recalcular o saldo dentro da mesma secao critica, contando somente reservas `Active` com `ExpiresAtUtc` posterior ao instante atual.
4. Rejeitar reserva ativa ja existente para o mesmo cliente-produto.
5. Rejeitar quantidade acima do saldo; caso contrario, criar a reserva ativa com vencimento em 72 horas.
6. Persistir e devolver a representacao da reserva criada.

## Cancelar reserva

Entrada: `ProductId` e `CustomerId` vindo de `X-Customer-Id`. O horario para validar se a reserva ainda pode ser cancelada vem de `TimeProvider` configurado no servidor.

O caso de uso cancela a reserva do par cliente-produto somente se ainda estiver dentro do prazo. Uma reserva vencida nao e cancelada nem expirada pelo caso de uso; o worker persiste a transicao depois. Ausencia de reserva cancelavel e sucesso idempotente; cliente ou produto inexistente continua sendo recurso nao encontrado.

## Abstracoes da Application

- `IAppDbContext` expoe os conjuntos de clientes, produtos e reservas e a persistencia assincrona.
- `IProductLock` representa a exclusao por produto necessaria para proteger alteracoes concorrentes; a implementacao em memoria fica na Infrastructure.
- `TimeProvider` fornece o instante UTC para validar cancelamento e calcular saldo; a criacao usa o UTC do servidor no construtor de `Reservation`.
- A expiracao persistida e responsabilidade de um worker futuro, fora dos casos de uso desta spec.

## Listar produtos

O caso de uso calcula a disponibilidade excluindo reservas com `ExpiresAtUtc` atingido, sem alterar o estado persistido delas. Cada item retorna a quantidade total, a quantidade reservada, o saldo e o status derivado.

## Listar reservas de cliente

O caso de uso retorna as reservas do cliente, inclusive as terminalmente canceladas ou expiradas, para tornar a exclusao logica auditavel. Uma reserva vencida pode continuar com estado persistido `Active` ate o worker processa-la; as datas identificam seu vencimento.

## Erros de negocio

- Quantidade ausente, zero ou negativa: validacao invalida.
- Header de cliente ausente ou invalido: validacao invalida.
- Cliente ou produto ausente: nao encontrado.
- Reserva ativa ja existente no mesmo cliente-produto: conflito.
- Saldo insuficiente: conflito por produto indisponivel.
