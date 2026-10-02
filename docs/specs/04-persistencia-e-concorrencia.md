# Persistencia, seed e concorrencia

**Status:** aceita  
**Dependencias:** [02-modelo-de-dominio.md](02-modelo-de-dominio.md), [03-casos-de-uso.md](03-casos-de-uso.md)

## Persistencia

O projeto usa EF Core com `Microsoft.EntityFrameworkCore.InMemory`. `AppDbContext` fica na Infrastructure e implementa `IAppDbContext` definido na Application. Cada entidade possui configuracao separada; a Application nao referencia EF Core concreto.

As reservas persistem seu estado terminal. Expirar ou cancelar e uma atualizacao de estado, nao uma exclusao fisica.

## Seed inicial

O seed deve ser deterministico e permitir testar a API sem preparacao manual.

| Tipo | Identificador estavel | Dados minimos |
| --- | --- | --- |
| Produto | Produto A | `TotalQuantity = 10` |
| Produto | Produto B | `TotalQuantity = 1` |
| Produto | Produto C | `TotalQuantity = 0` |
| Cliente | Cliente A | id e nome definidos no seed |
| Cliente | Cliente B | id e nome definidos no seed |

Os identificadores reais usados nos exemplos do README devem ser os mesmos do seed. O banco e reinicializado ao reiniciar a aplicacao, caracteristica aceita do provider InMemory.

## Garantia de concorrencia

O provider InMemory nao oferece a protecao transacional exigida para o saldo. A Application deve receber uma abstracao de exclusao por produto, implementada pela Infrastructure com sincronizacao em memoria por `ProductId`.

A secao critica cobre, nesta ordem: expirar reservas do produto, carregar reservas ativas, calcular saldo, validar quantidade, criar a nova reserva e persistir. Nenhuma outra reserva ou cancelamento para o mesmo produto pode atravessar esse intervalo. Operacoes em produtos diferentes nao devem se bloquear entre si.

Esta garantia vale para uma unica instancia do processo, que e o escopo do desafio com banco em memoria. Ela deve ser explicitamente declarada no README; nao se deve alegar suporte a multiplas instancias.
