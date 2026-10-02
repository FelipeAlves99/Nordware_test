# Processo e indice das specs

**Status:** aceita  
**Ultima atualizacao:** 2026-10-02

## Proposito

Esta pasta substitui qualquer uso futuro de ADRs. Cada spec combina a decisao necessaria, o comportamento esperado e os criterios que comprovam a implementacao. O [CONTEXT.md](../CONTEXT.md) continua sendo o resumo curto para retomar uma sessao.

## Ordem de desenvolvimento

| Ordem | Spec | Resultado esperado |
| --- | --- | --- |
| 1 | [01-requisitos-e-cenarios.md](01-requisitos-e-cenarios.md) | Regras e cenarios de aceitacao fechados. |
| 2 | [02-modelo-de-dominio.md](02-modelo-de-dominio.md) | Entidades, estados e invariantes implementaveis. |
| 3 | [03-casos-de-uso.md](03-casos-de-uso.md) | Contratos de comandos e consultas definidos. |
| 4 | [04-persistencia-e-concorrencia.md](04-persistencia-e-concorrencia.md) | EF InMemory, seed e protecao do estoque. |
| 5 | [05-expiracao-sob-demanda.md](05-expiracao-sob-demanda.md) | Expiracao observavelmente correta. |
| 6 | [06-contrato-http.md](06-contrato-http.md) | Endpoints e respostas estaveis. |
| 7 | [07-testes-e-entrega.md](07-testes-e-entrega.md) | Suite de testes e README completos. |

## Decisoes aceitas

| ID | Decisao |
| --- | --- |
| D-01 | `CONTEXT.md` e as specs aceitas sao a fonte de verdade; em conflito, a spec mais especifica prevalece. |
| D-02 | Sem autenticacao, a identidade do cliente vem do header `X-Customer-Id`. Isso nao e seguranca. |
| D-03 | Produtos possuem estoque e a solicitacao informa a quantidade a reservar. |
| D-04 | Cancelamento e permitido sem autorizacao, e idempotente; cancelamentos e expiracoes usam exclusao logica. |
| D-05 | A expiracao ocorre por validacao sob demanda, nunca por servico em background. |
| D-06 | A aplicacao, e nao o provider InMemory, protege o estoque contra concorrencia por produto. |
| D-07 | A aplicacao inicia com seed de clientes e produtos; `Produto A` possui 10 unidades. |
| D-08 | A entrega so esta pronta com testes de dominio, aplicacao e API, incluindo concorrencia e expiracao, alem do README. |

## Convencao de manutencao

- Uma spec passa por `rascunho`, `aceita` e `implementada`.
- Mude a spec antes ou junto do codigo quando uma regra mudar; nao esconda decisoes somente no codigo.
- Cada spec deve conter objetivo, regras, criterios de aceitacao e consequencias tecnicas quando aplicavel.
- Novas decisoes entram na spec mais proxima do assunto; nao e necessario criar um documento de decisao isolado.

## Convencao operacional adotada

Como o endpoint de cancelamento recebe somente o produto e o header do cliente, existe no maximo uma reserva **ativa** para o mesmo par cliente-produto. Para alterar a quantidade, o cliente cancela a reserva ativa e cria outra. Essa convencao evita um identificador de reserva adicional fora do contrato original.
