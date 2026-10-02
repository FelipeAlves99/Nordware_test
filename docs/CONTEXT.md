# Contexto do desafio - Reserva de produtos

> Este arquivo e o ponto de partida para futuras sessoes. Atualize-o sempre que uma decisao arquitetural ou requisito relevante mudar.

## Estado atual

- O repositorio contem somente o esqueleto da solucao e a documentacao inicial.
- O modelo de dominio foi implementado: `Customer`, `Product`, `Reservation`, estados de reserva e calculo de disponibilidade. Ha testes puros para invariantes, estados, cancelamento e expiracao.
- Persistencia, seed, casos de uso, endpoints, sincronizacao de concorrencia e expiracao sob demanda ainda nao foram implementados.
- O enunciado original esta preservado em [Desafio_NET_Core.pdf](Desafio_NET_Core.pdf).
- As decisoes e o fluxo de desenvolvimento ficam detalhados em [specs/00-processo-e-indice.md](specs/00-processo-e-indice.md).

## Objetivo

Construir uma API REST em C# / .NET 10 para que clientes de uma plataforma de e-commerce reservem produtos temporariamente indisponiveis ou de alta demanda.

## Requisitos funcionais do enunciado

- Entidades principais: `Customer`, `Product` e `Reservation`.
- Listar as reservas de um cliente: `GET /customer/{id_customer}/reservations`.
- Reservar um produto: `POST /products/{id}/reserve`, com a quantidade no corpo e o cliente no header temporario `X-Customer-Id`.
- Cancelar uma reserva ativa antes do vencimento (opcional): `DELETE /products/{id}/reserve`.
- Listar produtos e seus status (`disponivel`, `reservado` ou `indisponivel`): `GET /products`.
- Cada reserva dura 72 horas.
- Cada produto possui estoque; uma reserva informa quantas unidades pretende reter e so e aceita quando houver saldo suficiente no instante da solicitacao.
- Concorrencia: reservas simultaneas para o mesmo produto nao podem comprometer mais unidades que o estoque. Uma solicitacao sem saldo suficiente recebe erro de produto indisponivel.
- A expiracao e verificada sob demanda: antes de consultas ou operacoes do produto/reserva, reservas vencidas sao marcadas como expiradas e devolvem seu saldo ao produto. O mecanismo deve constar no README.
- O cancelamento nao exige autorizacao; identifica a reserva pelo produto e pelo `X-Customer-Id`, e e idempotente. Reservas expiradas ou canceladas permanecem armazenadas como exclusao logica.

## Requisitos tecnicos

- C# com .NET 10.
- Clean Architecture com camadas de dominio, aplicacao, infraestrutura e API separadas.
- A camada de dominio concentra entidades e regras de negocio; a aplicacao orquestra os casos de uso e depende de abstracoes; a infraestrutura implementa persistencia; a API fica limitada a HTTP e composicao.
- Aplicar orientacao a objetos e responsabilidade unica. Operacoes de dominio devem pertencer as classes adequadas.
- Persistencia com Entity Framework Core e `Microsoft.EntityFrameworkCore.InMemory`.
- Sem autenticacao.
- Sem PostgreSQL, migrations de banco relacional, containers ou dependencia de servicos externos.
- O README deve explicar como executar a aplicacao e documentar a estrategia de expiracao.

## Estrutura adotada

O formato segue o backend de referencia em `C:\repos\kitchen_menu`:

```text
backend/
  src/
    ProductReservation.Domain/          # entidades, value objects e regras puras
    ProductReservation.Application/     # comandos, queries, handlers, validacoes e abstracoes
    ProductReservation.Infrastructure/  # EF Core InMemory, DbContext e configuracoes
    ProductReservation.Api/             # Minimal API, grupos de endpoints e composicao
  tests/
    ProductReservation.Domain.Tests/
    ProductReservation.Application.Tests/
    ProductReservation.Api.Tests/
  ProductReservation.slnx
  Directory.Build.props
  Directory.Packages.props
```

### Regras de dependencia

- `Domain` nao depende de nenhuma outra camada.
- `Application` depende somente de `Domain`.
- `Infrastructure` depende de `Application` e `Domain`.
- `Api` depende de `Application` e `Infrastructure`, apenas para composicao e adaptacao HTTP.

## Convenções do projeto

- Minimal APIs, organizadas por grupos de endpoint, sem regra de negocio nos endpoints.
- CQRS com MediatR: cada comando ou query tera seu proprio diretorio e arquivos de request, handler e validator quando aplicavel.
- DI por extensoes `AddApplication()` e `AddInfrastructure()`.
- `IAppDbContext` na Application e `AppDbContext` na Infrastructure; configuracoes EF separadas por entidade.
- Nullable habilitado, implicit usings, warnings tratados como erros e versoes de pacotes centralizadas.
- Testes de dominio sem I/O, testes de aplicacao para handlers e testes de API para contratos HTTP e concorrencia.

## Decisoes a preservar na implementacao

- As rotas do enunciado sao o contrato inicial. Se for adotado versionamento `/v1`, documentar a compatibilidade antes de alterar essas rotas.
- Enquanto nao houver autenticacao, `X-Customer-Id` e a identidade declarada pelo cliente. Ele e um improviso deliberado e nao deve ser tratado como mecanismo de seguranca.
- Como o provider InMemory nao oferece as mesmas garantias transacionais de um banco relacional, a consistencia do estoque deve ser modelada e testada explicitamente na aplicacao, com sincronizacao por produto; nao presumir que o provider resolva concorrencia sozinho.
- O banco deve iniciar com seed de clientes e produtos, incluindo `Produto A` com 10 unidades.
- As specs em `docs/specs/` sao a fonte detalhada das decisoes de implementacao. Atualize a spec aplicavel e este resumo quando uma decisao aceita mudar.

## Fora de escopo neste momento

- Implementar casos de uso, endpoints ou seed de produtos.
- Autenticacao/autorizacao.
- PostgreSQL, migrations, Docker, observabilidade ou integracoes externas.

## Proximo passo sugerido

Seguir a ordem de `docs/specs/00-processo-e-indice.md`: casos de uso, persistencia/concorrencia, expiracao, HTTP e testes.
