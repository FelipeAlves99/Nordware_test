# Requisitos e cenarios de aceitacao

**Status:** aceita  
**Dependencia:** [00-processo-e-indice.md](00-processo-e-indice.md)

## Objetivo

Permitir que clientes reservem uma quantidade do estoque de um produto por 72 horas, consultem suas reservas e cancelem a reserva ativa. O enunciado original trata produtos como uma unidade; esta spec esclarece que o projeto trabalha com estoque e quantidade.

## Regras funcionais

- Uma reserva pertence a um cliente e a um produto, informa uma quantidade inteira positiva e vence 72 horas apos sua criacao.
- O saldo disponivel e `quantidade total - soma das quantidades de reservas ativas cujo ExpiresAtUtc ainda nao foi atingido`.
- Uma nova reserva so e aceita se sua quantidade for menor ou igual ao saldo disponivel no instante em que for processada.
- Um cliente pode criar multiplas reservas ativas para o mesmo produto, desde que cada solicitacao caiba no saldo disponivel no momento em que for processada.
- O status exibido do produto e `Available` enquanto ha saldo positivo, mesmo que parte do estoque esteja reservada; e `Reserved` quando todo o estoque positivo esta reservado; um produto com estoque total zero e `Unavailable`.
- O status `Available` indica que ainda e possivel criar reservas, limitado por `availableQuantity`; reservas parciais nao mudam o produto para `Reserved`.
- Uma reserva especifica e cancelada pelo seu proprio ID, exigindo `X-Customer-Id` igual ao cliente dono da reserva. Se nao existir reserva com esse ID, o cancelamento continua bem-sucedido por idempotencia; se pertencer a outro cliente, a API responde `400 Bad Request` com erro generico.
- Ao vencer, uma reserva deixa de consumir saldo imediatamente. Um worker a marca como `expirada` de forma assincrona; ate o worker processa-la, seu estado persistido pode continuar `Active`. Reservas canceladas e expiradas sao mantidas e nao consomem saldo.
- Sem autenticacao, qualquer consumidor pode declarar um `X-Customer-Id`. O header e tratado como permissao declarativa para cancelar apenas reservas do cliente informado, mas nao e uma garantia segura de identidade pois pode ser falsificado.

## Cenarios de aceitacao

### Reserva com saldo

Dado o `Produto A` com 10 unidades e nenhuma reserva ativa, quando o cliente cria uma reserva de 5 unidades, o produto continua `Available` com saldo 5; uma segunda reserva de 5 unidades tambem e aceita e entao o produto passa a `Reserved`.

### Reserva sem saldo suficiente

Dado um produto com saldo 2, quando o cliente solicita 3 unidades, entao nenhuma reserva e criada e a resposta informa indisponibilidade.

### Concorrencia por quantidade

Dado um produto com saldo 10, quando duas solicitacoes simultaneas pedem 6 unidades cada, entao somente uma e aceita. O saldo final nao pode ser negativo nem inferior a zero.

### Expiracao

Dada uma reserva que venceu, suas unidades deixam de consumir saldo pelo vencimento de `ExpiresAtUtc`, sem que a consulta ou operacao altere seu estado persistido. O worker marca a reserva como `expirada` posteriormente.

### Cancelamento idempotente

Dadas duas reservas ativas do mesmo cliente para o mesmo produto, quando o ID da primeira e cancelado, somente essa reserva fica `cancelada` e libera sua quantidade. Repetir o cancelamento desse ID continua sendo sucesso e nao altera a outra reserva.

Dada uma reserva pertencente a outro cliente, quando o cancelamento e solicitado com um `X-Customer-Id` diferente, a API responde `400 Bad Request` com erro generico e mantem a reserva inalterada.
