# Testes e definicao de pronto

**Status:** implementada
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
- Cancelamento exige o header do dono; outro cliente recebe erro `400` generico e a reserva permanece ativa.
- Relogio controlado verifica que uma reserva vencida deixa de consumir saldo sem que o caso de uso altere seu estado persistido.
- Cancelamento apos o vencimento nao muda o estado para `Cancelled`; a transicao para `Expired` e coberta com os testes do worker na etapa final.
- Seed contem `Produto A` com 10 unidades e clientes utilizaveis.

## Testes de API

- Contratos de rota, header, corpo e codigos HTTP definidos em [06-contrato-http.md](06-contrato-http.md).
- Listagem de produtos reflete quantidade total, reservada, saldo e status.
- Listagem de cliente mostra reservas ativas e terminalmente encerradas.
- Cem requests concorrentes de uma unidade nunca excedem o estoque; requests sem saldo recebem `409`.
- Scalar/OpenAPI e o contrato de autorizacao declarativa do cancelamento estao cobertos por testes de API.

## Definicao de pronto

- Solucao compila sem warnings tratados como erros e toda a suite passa.
- Nenhuma regra de negocio vive nos endpoints.
- Dependencias obedecem as camadas descritas em `CONTEXT.md`.
- O README explica o pre-requisito do .NET SDK, como executar, os identificadores do seed e exemplos de consulta, reserva, listagem e cancelamento.
- O README explica explicitamente a expiracao assincrona por worker, o retorno do saldo pelo vencimento e a janela ate a atualizacao do estado persistido.
