# Expiracao sob demanda

**Status:** aceita  
**Dependencias:** [03-casos-de-uso.md](03-casos-de-uso.md), [04-persistencia-e-concorrencia.md](04-persistencia-e-concorrencia.md)

## Decisao

Nao havera `BackgroundService`, timer ou processo externo. A expiracao e verificada sob demanda pelo relogio injetado, sempre em UTC.

## Pontos obrigatorios de verificacao

- Antes de reservar um produto, expirar reservas daquele produto dentro da exclusao por produto.
- Antes de cancelar, expirar reservas daquele produto dentro da exclusao por produto.
- Antes de listar produtos, expirar as reservas relevantes antes de derivar saldo e status.
- Antes de listar as reservas de um cliente, expirar as reservas daquele cliente antes de devolve-las.

Uma reserva vence quando `now >= ExpiresAtUtc`. Ao vencer, passa uma unica vez para `Expired`, recebe o horario de expiracao e deixa de contribuir para a quantidade reservada.

## Consequencias

- Uma reserva pode continuar gravada como `Active` enquanto nenhuma operacao a observar; isso nao e um erro, pois o primeiro caso de uso relevante a expira antes de responder.
- Toda resposta observavel apos o vencimento deve refletir o saldo devolvido e o estado `Expired`.
- Testes devem controlar o relogio, sem depender de espera real de 72 horas.
- O README explicara o mecanismo e sua limitacao observacional.
