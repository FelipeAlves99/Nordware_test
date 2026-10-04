# Testes e definicao de pronto

**Status:** aceita  
**Dependencias:** todas as specs anteriores.

## Testes de dominio

- Criacao de reserva com quantidade positiva e vencimento de 72 horas.
- Transicoes validas para cancelada e expirada, sem reativacao.
- Derivacao de saldo e dos tres status de produto.
- Rejeicao de quantidade invalida ou maior que o saldo calculado.

## Testes de aplicacao

- Cliente/produto inexistentes, header invalido, multiplas reservas para o mesmo cliente-produto e saldo insuficiente agregado.
- Cancelamento libera saldo, e repeticao e idempotente.
- Cancelar por ID afeta apenas aquela reserva quando ha varias reservas do mesmo cliente para o produto.
- Relogio controlado verifica que uma reserva vencida deixa de consumir saldo sem que o caso de uso altere seu estado persistido.
- Cancelamento apos o vencimento nao muda o estado para `Cancelled`; a transicao para `Expired` e coberta com os testes do worker na etapa final.
- Seed contem `Produto A` com 10 unidades e clientes utilizaveis.

## Testes de API

- Contratos de rota, header, corpo e codigos HTTP definidos em [06-contrato-http.md](06-contrato-http.md).
- Listagem de produtos reflete quantidade total, reservada, saldo e status.
- Listagem de cliente mostra reservas ativas e terminalmente encerradas.
- Duas requisicoes concorrentes cujo total excede o saldo resultam em reservas aceitas que nunca ultrapassam o estoque; a excedente recebe `409`.

## Definicao de pronto

- Solucao compila sem warnings tratados como erros e toda a suite passa.
- Nenhuma regra de negocio vive nos endpoints.
- Dependencias obedecem as camadas descritas em `CONTEXT.md`.
- O README explica pre-requisitos, como executar, os identificadores do seed, exemplos de chamadas e a limitacao da concorrencia em processo unico.
- O README explica explicitamente a expiracao assincrona por worker, o retorno do saldo pelo vencimento e a janela ate a atualizacao do estado persistido.
