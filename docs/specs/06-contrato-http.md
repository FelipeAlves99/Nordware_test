# Contrato HTTP

**Status:** aceita  
**Dependencias:** [01-requisitos-e-cenarios.md](01-requisitos-e-cenarios.md), [03-casos-de-uso.md](03-casos-de-uso.md)

## Convencoes gerais

- As rotas permanecem sem versao enquanto forem o contrato inicial do desafio.
- `X-Customer-Id` e obrigatorio em operacoes que atuam em nome de um cliente. O valor deve usar o formato do identificador de `Customer` escolhido na implementacao.
- Datas usam ISO 8601 em UTC. Erros usam Problem Details ou formato equivalente, com codigo de negocio estavel.

## Endpoints

### `GET /products`

Retorna `200 OK` com todos os produtos. Cada item contem, no minimo, `id`, `name`, `totalQuantity`, `reservedQuantity`, `availableQuantity` e `status` (`disponivel`, `reservado` ou `indisponivel`). A consulta aplica expiracao sob demanda antes de responder.

### `POST /products/{id}/reserve`

Header obrigatorio: `X-Customer-Id`.

Corpo:

```json
{ "quantity": 3 }
```

Retorna `201 Created` com `id`, `customerId`, `productId`, `quantity`, `status`, `createdAtUtc` e `expiresAtUtc`. Retorna `400` para header ou quantidade invalidos, `404` para cliente/produto inexistente e `409` para reserva ativa duplicada ou saldo insuficiente.

### `DELETE /products/{id}/reserve`

Header obrigatorio: `X-Customer-Id`.

Retorna `204 No Content` tanto ao cancelar uma reserva ativa quanto quando nao existe reserva ativa para o par cliente-produto. Retorna `400` para header invalido e `404` para cliente/produto inexistente.

### `GET /customer/{id_customer}/reservations`

Retorna `200 OK` com as reservas do cliente, incluindo `quantity`, estado e datas de criacao, vencimento e encerramento quando houver. Retorna `404` caso o cliente nao exista. Como nao ha autenticacao, a rota nao compara o identificador com um header.

## Compatibilidade

O enunciado nao especifica payloads nem codigos de resposta. Estas escolhas sao o contrato do projeto. Qualquer versao futura, rota adicional ou mudanca de semantica deve ser registrada aqui antes de alterar a API.
