# Expiracao por worker

**Status:** aceita
**Implementacao:** deferida para a etapa final (ordem 7 no indice).
**Dependencias:** [03-casos-de-uso.md](03-casos-de-uso.md), [04-persistencia-e-concorrencia.md](04-persistencia-e-concorrencia.md)

## Decisao

Um worker separado sera responsavel por persistir a transicao de reservas vencidas para `Expired`. A implementacao do worker fica para a ultima etapa do projeto; os casos de uso nao executam expiracao sob demanda.

## Regras enquanto o worker nao processa a reserva

- Uma reserva deixa de consumir saldo assim que `now >= ExpiresAtUtc`, mesmo se seu estado persistido ainda for `Active`.
- Os casos de uso filtram reservas vencidas ao validar duplicidade e calcular saldo, sem alterar seus estados.
- Cancelamento depois do vencimento nao cancela nem expira a reserva; permanece idempotente e o worker a atualiza posteriormente.
- A consulta de reservas retorna o estado persistido. Assim, uma reserva vencida pode aparecer como `Active` ate o worker processa-la; `ExpiresAtUtc` informa o vencimento.

## Responsabilidades do worker

- Selecionar reservas `Active` com `ExpiresAtUtc <= now` e aplicar a transicao de dominio para `Expired`.
- Definir `ExpiredAtUtc` com o instante UTC em que o worker processa a transicao.
- Tornar o processamento idempotente e coordenar-se pela mesma exclusao por produto usada por reserva e cancelamento.

- Frequencia, tamanho dos lotes, politica de retry e comportamento de encerramento serao decididos durante a implementacao final do worker.
- Testes devem controlar o relogio e cobrir atraso, repeticao e concorrencia, sem depender de espera real de 72 horas.
- O README documentara o processamento assincrono e a janela em que o estado persistido pode continuar `Active` depois do vencimento.
