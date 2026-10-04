# Expiracao por worker

**Status:** implementada
**Implementacao:** Quartz no host da API; execucao imediata ao iniciar e cron configuravel via `appsettings.json` (padrao a cada minuto).
**Dependencias:** [03-casos-de-uso.md](03-casos-de-uso.md), [04-persistencia-e-concorrencia.md](04-persistencia-e-concorrencia.md)

## Decisao

Um job Quartz no host sera responsavel por persistir a transicao de reservas vencidas para `Expired`. Os casos de uso nao executam expiracao sob demanda. O job dispara uma vez ao iniciar o host e depois de acordo com `Quartz:ReservationExpiration:CronExpression`, definido em `appsettings.json`. O valor padrao `0 * * * * ?` dispara no inicio de cada minuto.

## Regras enquanto o worker nao processa a reserva

- Uma reserva deixa de consumir saldo assim que `now >= ExpiresAtUtc`, mesmo se seu estado persistido ainda for `Active`.
- Os casos de uso filtram reservas vencidas ao validar duplicidade e calcular saldo, sem alterar seus estados.
- Cancelamento depois do vencimento nao cancela nem expira a reserva; permanece idempotente e o worker a atualiza posteriormente.
- A consulta de reservas retorna o estado persistido. Assim, uma reserva vencida pode aparecer como `Active` ate o worker processa-la; `ExpiresAtUtc` informa o vencimento.

## Responsabilidades do worker

- Selecionar reservas `Active` com `ExpiresAtUtc <= now` e aplicar a transicao de dominio para `Expired`.
- Capturar um unico instante UTC por lote e usa-lo como limite de vencimento e valor de `ExpiredAtUtc`.
- Processar no maximo 100 reservas por execucao, em ordem de vencimento, sob o mesmo lock por produto usado por reserva e cancelamento.
- Tornar o processamento idempotente: uma nova execucao ignora estados que ja nao sejam `Active`.
- Em falha, tentar novamente em 1, 5 e 15 minutos; se as tentativas falharem, registrar a falha e deixar a proxima execucao horaria tentar de novo.
- No encerramento gracioso, o host aguarda o job e solicita cancelamento; operacoes ja persistidas ficam validas e o proximo disparo processa o restante.
- Testes controlam o relogio e cobrem vencimento no limite, atraso, repeticao, tamanho de lote e concorrencia, sem esperar 72 horas reais.
- O README documenta o processamento assincrono e a janela em que o estado persistido pode continuar `Active` depois do vencimento.

O `ExecuteUpdateAsync` esta registrado apenas como alternativa comentada para um futuro provider relacional. O provider InMemory atual nao suporta esse update em lote.
