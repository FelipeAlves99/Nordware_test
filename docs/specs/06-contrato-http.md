# Contrato HTTP

**Status:** implementada
**Dependencias:** [01-requisitos-e-cenarios.md](01-requisitos-e-cenarios.md), [03-casos-de-uso.md](03-casos-de-uso.md)

## Convencoes gerais

- As rotas permanecem sem versao enquanto forem o contrato inicial do desafio.
- `X-Customer-Id` e obrigatorio para criar e cancelar uma reserva. No cancelamento, funciona como um token de permissao que deve corresponder ao cliente dono da reserva; nao e autenticacao segura porque o chamador pode declarar qualquer GUID.
- Datas usam ISO 8601 em UTC. Erros usam RFC Problem Details (`application/problem+json`) com codigo de negocio estavel na propriedade `code`.

## Endpoints

### `GET /products`

Retorna `200 OK` com todos os produtos. Cada item contem, no minimo, `id`, `name`, `totalQuantity`, `reservedQuantity`, `availableQuantity` e `status` (`Available`, `Reserved` ou `Unavailable`), correspondendo ao identificador do status de dominio. `Available` permanece enquanto houver saldo, inclusive com reservas parciais; `Reserved` significa que todo o estoque positivo esta reservado; estoque total zero e `Unavailable`. O saldo exclui reservas cujo prazo venceu, mesmo que o worker ainda nao tenha persistido o estado `Expired`.

### `POST /products/{id}/reserve`

Header obrigatorio: `X-Customer-Id`.

Corpo:

```json
{ "quantity": 3 }
```

Retorna `201 Created` com `id`, `customerId`, `productId`, `quantity`, `status`, `createdAtUtc`, `expiresAtUtc` e as datas terminais anulaveis. Cada solicitacao cria sua propria reserva; podem existir varias reservas ativas para o mesmo cliente e produto. Retorna `400` para header, identificador ou quantidade invalidos, `404` para cliente/produto inexistente e `409` para saldo insuficiente.

### `DELETE /reservations/{reservationId}`

Header obrigatorio: `X-Customer-Id`.

Cancela exclusivamente a reserva identificada pelo ID quando o `X-Customer-Id` corresponde ao cliente dono. Retorna `204 No Content` quando ela foi cancelada, ja estava terminal ou nao existe. Retorna `400 Bad Request` para ID/header ausente ou invalido e, com um corpo generico `InvalidRequest`, quando o cliente declarado nao e o dono. Essa verificacao e uma autorizacao baseada na identidade declarada; sem autenticacao, nao constitui uma protecao confiavel contra falsificacao do header.

### `GET /customer/{id_customer}/reservations`

Retorna `200 OK` com as reservas do cliente, incluindo `quantity`, estado persistido e datas de criacao, vencimento e encerramento quando houver. Uma reserva vencida pode continuar como `Active` ate ser processada pelo worker. Retorna `400` para identificador malformado e `404` caso o cliente nao exista. Como nao ha autenticacao, a rota nao compara o identificador com um header.

## Compatibilidade

O enunciado nao especifica payloads nem codigos de resposta. Estas escolhas sao o contrato do projeto. Qualquer versao futura, rota adicional ou mudanca de semantica deve ser registrada aqui antes de alterar a API.
