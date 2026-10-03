# Requisitos e cenarios de aceitacao

**Status:** aceita  
**Dependencia:** [00-processo-e-indice.md](00-processo-e-indice.md)

## Objetivo

Permitir que clientes reservem uma quantidade do estoque de um produto por 72 horas, consultem suas reservas e cancelem a reserva ativa. O enunciado original trata produtos como uma unidade; esta spec esclarece que o projeto trabalha com estoque e quantidade.

## Regras funcionais

- Uma reserva pertence a um cliente e a um produto, informa uma quantidade inteira positiva e vence 72 horas apos sua criacao.
- O saldo disponivel e `quantidade total - soma das quantidades de reservas ativas cujo ExpiresAtUtc ainda nao foi atingido`.
- Uma nova reserva so e aceita se sua quantidade for menor ou igual ao saldo disponivel no instante em que for processada.
- Um cliente possui no maximo uma reserva ativa para o mesmo produto.
- O status exibido do produto e `disponivel` quando nao ha unidades reservadas, `reservado` quando ha saldo e ao menos uma unidade reservada, e `indisponivel` quando o saldo e zero.
- Cancelar a reserva ativa do par cliente-produto libera integralmente sua quantidade. Sem reserva ativa, a operacao continua bem-sucedida.
- Ao vencer, uma reserva deixa de consumir saldo imediatamente. Um worker a marca como `expirada` de forma assincrona; ate o worker processa-la, seu estado persistido pode continuar `Active`. Reservas canceladas e expiradas sao mantidas e nao consomem saldo.
- Sem autenticacao, qualquer consumidor pode declarar um `X-Customer-Id`; portanto, o sistema nao aplica regra de propriedade ou permissao.

## Cenarios de aceitacao

### Reserva com saldo

Dado o `Produto A` com 10 unidades e nenhuma reserva ativa, quando o cliente informa quantidade 3, entao a reserva e criada com 3 unidades, vencimento em 72 horas e o produto passa a `reservado`, com saldo 7.

### Reserva sem saldo suficiente

Dado um produto com saldo 2, quando o cliente solicita 3 unidades, entao nenhuma reserva e criada e a resposta informa indisponibilidade.

### Concorrencia por quantidade

Dado um produto com saldo 10, quando duas solicitacoes simultaneas pedem 6 unidades cada, entao somente uma e aceita. O saldo final nao pode ser negativo nem inferior a zero.

### Expiracao

Dada uma reserva que venceu, suas unidades deixam de consumir saldo pelo vencimento de `ExpiresAtUtc`, sem que a consulta ou operacao altere seu estado persistido. O worker marca a reserva como `expirada` posteriormente.

### Cancelamento idempotente

Dada uma reserva ativa para o par cliente-produto, quando ela e cancelada, entao fica `cancelada` e libera o saldo. Quando o mesmo cancelamento e repetido, a resposta continua sendo sucesso e o saldo nao muda novamente.
