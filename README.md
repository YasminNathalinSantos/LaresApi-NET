<div align="center">

# 🏘️ Lares — API .NET

**Global Solution 2026/2 · CitizenTech · Advanced Business Development with .NET**

![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![Minimal API](https://img.shields.io/badge/ASP.NET_Core-Minimal_API-512BD4)
![EF Core](https://img.shields.io/badge/EF_Core-10-6C3483)
![Azure SQL](https://img.shields.io/badge/Azure-SQL-0078D4?logo=microsoftazure&logoColor=white)
![JWT](https://img.shields.io/badge/Auth-JWT-000000?logo=jsonwebtokens)
![xUnit](https://img.shields.io/badge/Testes-xUnit-5E5E5E)

O cidadão registra um problema na rua. A prefeitura prioriza, define o prazo e atualiza o status.<br>
O cidadão acompanha tudo pela linha do tempo da ocorrência.

</div>

---

## 📌 Sumário

- [Integrantes](#-integrantes)
- [Relação com o tema CitizenTech](#-relação-com-o-tema-citizentech)
- [Como funciona](#-como-funciona)
- [Tecnologias](#tecnologias)
- [Como rodar](#como-rodar)
- [Endpoints](#endpoints)
- [Regras de negócio](#regras-de-negócio)
- [Requisitos atendidos](#-requisitos-atendidos)

## 👥 Integrantes

| Integrante | RM |
| --- | --- |
| Felipe Bezerra Beatrici | RM 564723 |
| Max Hayashi Batista | RM 563717 |
| Henrique Cunha Torres | RM 565119 |
| Lucas da Silva Lima | RM 562118 |
| Yasmin Nathalin Miranda dos Santos | RM 561365 |

*Lares* eram os deuses romanos que protegiam os bairros; "lares" também são as casas de quem mora ali.

--- | --- |
| Felipe Bezerra Beatrici | RM 564723 |
| Max Hayashi Batista | RM 563717 |
| Henrique Cunha Torres | RM 565119 |
| Lucas da Silva Lima | RM 562118 |
| Yasmin Nathalin Miranda dos Santos | RM 561365 |

---

## 🌱 Relação com o tema CitizenTech

- **ODS 11 — Cidades e comunidades sustentáveis:** canal único entre morador e prefeitura para manutenção urbana.
- **ODS 10 — Redução das desigualdades:** qualquer bairro registra e acompanha com a mesma transparência; o filtro por bairro mostra onde a cidade atende mais devagar.
- **Valor real para o cidadão:** em vez de ligar e "torcer", ele vê o status, a prioridade, o prazo estimado e cada mudança no histórico.
- **Métricas de impacto:** tempo médio de atendimento e ocorrências resolvidas por bairro (`GET /ocorrencias?bairro=...&status=RESOLVIDA`).

---

## 🔄 Como funciona

```mermaid
flowchart LR
    A[Cidadão registra<br>RECEBIDA] --> B[Gestor faz a triagem<br>categoria, prioridade, prazo<br>TRIADA]
    B --> C[Equipe atende<br>EM_ANDAMENTO]
    A -. atalho .-> C
    C --> D[RESOLVIDA]
```

| Papel | O que faz |
| --- | --- |
| `CIDADAO` | cria conta, registra, edita/exclui enquanto não começou o atendimento, acompanha |
| `GESTOR` | vê a fila por prioridade, faz a triagem, muda o status e ajusta o prazo |

---

## Tecnologias

| Item | Uso |
| --- | --- |
| .NET 10 · **ASP.NET Core Minimal API** (`MapGroup`) | endpoints REST |
| **EF Core 10** + **SQL Server** (`Microsoft.EntityFrameworkCore.SqlServer`) | persistência local ou Azure SQL |
| **JWT Bearer** + BCrypt | autenticação e papéis `CIDADAO` / `GESTOR` |
| **OpenAPI** + Swagger UI + Scalar | documentação interativa com botão **Authorize** |
| `Microsoft.AspNetCore.RateLimiting` | rate limiting com janela fixa e HTTP 429 |
| Health Checks | `/health`, `/health/live`, `/health/ready` (conectividade SQL Server) |
| **xUnit** + `WebApplicationFactory` | testes unitários e de integração **sem banco** (EF Core InMemory) |

---

## Arquitetura (camadas)

```
src/LaresApi
├── Program.cs
├── Endpoints/      Minimal API (MapGroup)
├── Services/       regras de aplicação, validações, idempotência, JWT
├── Domain/         regras de status, constantes, exceções
├── Models/         entidades do domínio
├── Data/           LaresDbContext (EF Core + SQL Server), migrations e DbSeeder
├── DTOs/           contratos JSON de entrada e saída
├── Middleware/     GlobalExceptionHandler
├── HealthChecks/   api, memória e SQL Server
├── Extensions/     registro dos serviços e inicialização do banco
├── OpenApi/        esquema Bearer (botão Authorize)
└── Settings/       Jwt, LaresDatabase, RateLimiting
tests/LaresApi.Tests
├── Unit/
├── Integration/
└── Fakes/
```

### Banco de dados: SQL Server / Azure SQL

O acesso usa **EF Core 10** com `Microsoft.EntityFrameworkCore.SqlServer`. O mesmo `LaresDbContext` funciona com SQL Server/Azure SQL e com EF Core InMemory nos testes. As migrations ficam em `src/LaresApi/Data/Migrations`; a aplicação aplica as migrations relacionais na inicialização e depois executa o seed.

| Tabela | Conteúdo |
| --- | --- |
| `LARES_USUARIO` | nome, email (único), hash BCrypt e papel |
| `LARES_CATEGORIA` | código (único), nome, descrição e prazo padrão |
| `LARES_OCORRENCIA` | dados da ocorrência e cópias do nome do cidadão/categoria usadas pela API |
| `LARES_HISTORICO_STATUS` | histórico relacional, ligado à ocorrência e com exclusão em cascata |
| `LARES_CHAVE_IDEMPOTENCIA` | chave, hash da requisição e resposta armazenada por 24 h |

O histórico continua sendo uma coleção no modelo C# (`OwnsMany`), mas é armazenado em uma tabela própria para SQL. As colunas usam nomes `snake_case`; as strings de IDs permanecem com 24 caracteres hexadecimais para preservar o formato já exposto pela API. `LARES_CHAVE_IDEMPOTENCIA` e algumas colunas auxiliares também são necessárias para manter o comportamento atual, além das quatro tabelas compartilhadas descritas no material da disciplina.

### Triagem pelo gestor

A prioridade e o prazo são definidos pelo gestor no `PATCH /ocorrencias/{id}/status`:

- ao **triar** (sair de `RECEBIDA`), o gestor informa `prioridade` (`BAIXA`, `MEDIA`, `ALTA`) e `categoria` (código, ex.: `BURACO`);
- se não mandar `prazoEstimadoDias`, a API usa o **prazo padrão da categoria** (BURACO = 15 dias);
- em qualquer mudança seguinte ele pode ajustar prioridade, prazo e categoria.

---

## Como rodar

Pré-requisitos: [.NET SDK 10](https://dotnet.microsoft.com/download). Para persistência SQL, use SQL Server local ou Azure SQL. Sem um banco local configurado, o perfil `Development` usa EF Core InMemory.

```bash
git clone https://github.com/SEU-USUARIO/LaresApi-NET.git
cd LaresApi-NET
dotnet restore

cd src/LaresApi
dotnet user-secrets set "ConnectionStrings:LaresSql" "Server=localhost;Database=LaresDb;User Id=<usuario>;Password=<senha>;TrustServerCertificate=True"
dotnet user-secrets set "LaresDatabase:UsarEmMemoria" "false"
dotnet user-secrets set "Jwt:Chave" "<texto aleatório com 32+ caracteres>"
cd ../..

dotnet run --project src/LaresApi
```

Abra **http://localhost:5102/swagger** (ou `/scalar`). Com SQL Server configurado, a primeira subida aplica as migrations e cria o gestor, o cidadão de teste e as 7 categorias. No perfil `Development`, a aplicação usa InMemory por padrão para facilitar execução local sem SQL.

Em produção (Azure), os segredos vão em variáveis de ambiente:

| Variável | Valor |
| --- | --- |
| `ConnectionStrings__LaresSql` | connection string do SQL Server/Azure SQL |
| `Jwt__Chave` | texto aleatório com 32+ caracteres |

Configure `ConnectionStrings__LaresSql` como **Application Setting** no Azure App Service (ou use User Secrets localmente). Não coloque a connection string nem a chave JWT no repositório. Fora de `Development`, a API falha explicitamente se SQL Server estiver selecionado sem essa configuração; o uso de InMemory deve ser habilitado explicitamente com `LaresDatabase__UsarEmMemoria=true`.

Para criar/atualizar o schema manualmente, a partir da raiz do repositório:

```bash
dotnet tool update --global dotnet-ef --version 10.0.0
dotnet ef database update --project src/LaresApi/LaresApi.csproj --startup-project src/LaresApi/LaresApi.csproj
dotnet ef migrations script --project src/LaresApi/LaresApi.csproj --startup-project src/LaresApi/LaresApi.csproj --output scripts/script-bd.sql
```

O comando `database update` exige uma connection string real disponível em `ConnectionStrings:LaresSql` (User Secrets ou `ConnectionStrings__LaresSql`). A migration inicial e o script gerado versionam a criação das tabelas do SQL Server.

### Testes

```bash
dotnet restore
dotnet build
dotnet test
```

Os testes não precisam de Azure nem de SQL Server: o `CustomWebApplicationFactory` sobe a API com o provider InMemory do EF Core.

---

## Usuários de teste (seed)

| Papel | E-mail | Senha |
| --- | --- | --- |
| GESTOR | `gestor@lares.com` | `Gestor@123` |
| CIDADAO | `cidadao@lares.com` | `Cidadao@123` |

No Swagger: `POST /auth/login` → copie o `token` → **Authorize** → cole o token.

---

## Endpoints

| Método e rota | Quem | Para quê |
| --- | --- | --- |
| `POST /auth/registro` | público | cadastro de cidadão |
| `POST /auth/login` | público | devolve o token (24 h) |
| `GET /usuarios/me` | logado | dados do usuário |
| `GET /categorias` | logado | categorias e prazo padrão |
| `POST /ocorrencias` | CIDADAO | registrar (idempotente com `Idempotency-Key`, 5/min) |
| `GET /ocorrencias/minhas?page&size` | CIDADAO | minhas ocorrências |
| `GET /ocorrencias?bairro&status&sort&page&size` | logado | busca; fila do gestor: `status=RECEBIDA&sort=prioridade` |
| `GET /ocorrencias/{id}` | logado | detalhe com histórico |
| `PUT /ocorrencias/{id}` | CIDADAO dono | editar (só `RECEBIDA` ou `TRIADA`) |
| `DELETE /ocorrencias/{id}` | CIDADAO dono | excluir (só `RECEBIDA` ou `TRIADA`) |
| `PATCH /ocorrencias/{id}/status` | GESTOR | mudar status, **definir prioridade e prazo**, corrigir categoria |
| `GET /health` · `/health/live` · `/health/ready` | público | monitoramento (inclui conectividade com SQL Server) |

**Códigos:** 200, 201, 204, 400 (validação), 401 (sem token), 403 (papel errado ou não é o dono), 404, 409 (status não permite), 422 (Idempotency-Key reaproveitada com outro corpo), 429 (limite de requisições).

### Exemplos

```jsonc
// PATCH /ocorrencias/{id}/status  (gestor faz a triagem)
{ "status": "TRIADA", "categoria": "BURACO", "prioridade": "ALTA", "comentario": "Perto de escola" }
// → prazoEstimadoDias = 15 (padrão de BURACO)

// PATCH /ocorrencias/{id}/status  (gestor envia equipe e ajusta o prazo)
{ "status": "EM_ANDAMENTO", "prazoEstimadoDias": 10, "comentario": "Equipe enviada" }

// Erro (todas as respostas de erro)
{ "status": 400, "erro": "Bad Request", "mensagem": "Dados inválidos",
  "campos": { "descricao": "não pode ser vazio" } }
```

Mais exemplos em `src/LaresApi/LaresApi.http`.

---

## Regras de negócio

**Status:** `RECEBIDA` → `TRIADA` → `EM_ANDAMENTO` → `RESOLVIDA`, com o atalho `RECEBIDA` → `EM_ANDAMENTO`. Qualquer outra transição devolve **409**. Toda mudança, inclusive a abertura, grava uma linha em `LARES_HISTORICO_STATUS`.

**Cidadão** só edita ou exclui a própria ocorrência enquanto ela está `RECEBIDA` ou `TRIADA`.

**Idempotência (`POST /ocorrencias`):** com o header `Idempotency-Key`, a API reserva a chave em `LARES_CHAVE_IDEMPOTENCIA` **antes** de criar a ocorrência e guarda a resposta. Repetir com a mesma chave e o mesmo corpo devolve a **mesma resposta** (header `Idempotency-Replayed: true`) sem criar outro registro; a mesma chave com outro corpo devolve **422**; se a primeira ainda está processando, **409**. A chave vale por usuário e expira em 24 h.

**Rate limiting:** janela fixa. `POST /ocorrencias`: 5 por minuto **por usuário**. `/auth/*`: 10 por minuto por IP (protege contra força bruta no login). Acima disso: **429** com `Retry-After`.

**Health checks:** `api` (processo de pé), `memoria` (Degraded acima de 512 MB) e `sqlserver` (conectividade com o banco configurado). `/health/live` verifica apenas o processo; `/health/ready` verifica o banco.

---

## ✅ Requisitos atendidos

| # | Requisito | Onde está |
| --- | --- | --- |
| 01 | Minimal API RESTful, validações, códigos HTTP, OpenAPI/Swagger | `Endpoints/`, `Services/Validacoes.cs`, `/swagger` |
| 02 | Autenticação e autorização com JWT | `Services/TokenService.cs`, políticas `SomenteCidadao` e `SomenteGestor` |
| 03 | Idempotência | header `Idempotency-Key`, `Services/Idempotencia.cs` |
| 04 | Rate limiting com HTTP 429 | `AddRateLimiter` em `Extensions/ServiceCollectionExtensions.cs` |
| 05 | Testes unitários e de integração com xUnit | `tests/LaresApi.Tests` (76 casos) |
| 06 | Health checks da API, banco e serviços externos | `/health`, `/health/live`, `/health/ready` |

Tecnologias obrigatórias: **ASP.NET Core Minimal API** e **EF Core** com provider SQL Server.

---

## Uso de IA

O Lares usa IA na **API Java** (serviço Python/FastAPI de triagem e Spring AI para o resumo ao cidadão). Na **API .NET** a triagem é **feita pelo gestor**: ele define prioridade, categoria e prazo no `PATCH /ocorrencias/{id}/status`, e a API sugere o prazo padrão da categoria. Assim a .NET não depende de um serviço externo que dorme no plano gratuito do Render.

---

## DevOps e Cloud — Azure

A infraestrutura e o processo de CI/CD do **Lares** foram implementados utilizando **Microsoft Azure** e **Azure DevOps**.

A API utilizada nesta entrega é a **Lares API .NET**, publicada em um **Azure App Service Linux** e conectada a um banco **Azure SQL Database**.

O Azure DevOps é utilizado para integrar planejamento, versionamento e automação por meio de:

- **Azure Boards** — rastreamento das tarefas e vínculo com branches, commits e Pull Requests;
- **Azure Repos** — versionamento do código utilizado pelas pipelines;
- **Azure Pipelines** — execução automática das pipelines de CI e CD.

### Arquitetura DevOps

![Arquitetura DevOps do Lares](docs/arquitetura-devops.png)

A infraestrutura possui os seguintes componentes principais:

- **Resource Group** — agrupa os recursos utilizados pelo projeto no Azure;
- **App Service Plan Linux** — define o ambiente e os recursos utilizados pelo Web App;
- **Azure App Service** — hospeda a API .NET;
- **Azure SQL Server** — servidor lógico que hospeda o banco;
- **Azure SQL Database** — banco relacional utilizado pela API;
- **Firewall Rules** — controlam o acesso ao Azure SQL;
- **Azure DevOps** — responsável pelo versionamento, rastreabilidade e CI/CD.

A infraestrutura é criada por meio de **Azure CLI**, utilizando o script disponível em:

```text
/scripts/01_criacao_infra.sh
```

O DDL utilizado pelo banco está disponível em:

```text
/scripts/script-bd.sql
```

---

## Fluxo de trabalho com Azure Boards e Repos

O desenvolvimento segue um fluxo baseado em Task, Branch e Pull Request.

1. Uma **Task** é criada no Azure Boards.
2. Uma nova branch é criada e vinculada ao Work Item.
3. As alterações de código são feitas nessa branch.
4. O commit é vinculado à Task.
5. É criado um Pull Request para a branch `main`.
6. A `main` possui políticas de proteção que exigem:
   - Work Item vinculado;
   - aprovação de reviewer;
   - utilização de Pull Request.
7. Após a aprovação, o código é mergeado na `main`.
8. O merge dispara automaticamente a pipeline de CI.

Esse processo fornece rastreabilidade entre:

```text
Task → Branch → Commit → Pull Request → Merge → CI/CD
```

---

## Pipeline de CI — Continuous Integration

A pipeline de CI é acionada automaticamente após alterações aprovadas e mergeadas na branch principal.

Ela possui duas etapas principais.

### 1. Provisionamento da infraestrutura

A Task **Azure CLI** executa o script:

```text
scripts/01_criacao_infra.sh
```

O script cria ou valida os recursos necessários no Microsoft Azure, incluindo:

- Resource Group;
- App Service Plan;
- Web App;
- Azure SQL Server;
- Azure SQL Database;
- regras de firewall.

Dados sensíveis, como a senha administrativa do SQL, são armazenados como **variáveis secretas no Azure DevOps** e não são versionados no repositório.

### 2. Build and Test

Após a infraestrutura, a pipeline executa as seguintes Tasks:

#### Restore

Restaura os pacotes e dependências NuGet necessários para a solução.

#### Build

Compila a solução .NET e valida a existência de erros de compilação.

#### Test

Executa automaticamente os testes desenvolvidos com **xUnit**.

Os resultados são publicados no próprio Azure DevOps, permitindo visualizar quantidade de testes executados, aprovados e falhos.

#### Publish

Prepara a aplicação compilada para publicação.

#### Publish Artifact

Publica o pacote gerado pela aplicação como artefato da pipeline.

O artefato gerado é:

```text
LaresApi.zip
```

Esse pacote é posteriormente utilizado pela pipeline de Release/CD.

---

## Pipeline de CD — Continuous Deployment

Após a geração de um novo artefato pela CI, o **Continuous Deployment Trigger** inicia automaticamente uma nova Release.

A pipeline de CD:

1. baixa o artefato `LaresApi.zip`;
2. executa a Task de deploy para o Azure App Service;
3. aplica as configurações e variáveis de ambiente;
4. publica a nova versão da API no Web App.

O resultado é a atualização automática da aplicação hospedada no Azure sempre que uma nova versão aprovada chega à branch principal.

O fluxo completo pode ser resumido como:

```text
Azure Boards
      ↓
Branch de desenvolvimento
      ↓
Commit
      ↓
Pull Request
      ↓
Merge na main
      ↓
CI automática
      ↓
Restore
      ↓
Build
      ↓
Testes xUnit
      ↓
Publish
      ↓
LaresApi.zip
      ↓
Release automática
      ↓
Deploy no Azure App Service
      ↓
API .NET
      ↓
Azure SQL Database
```

---

## Variáveis de ambiente e segurança

Dados sensíveis não são armazenados diretamente no código-fonte.

As principais configurações utilizadas no ambiente Azure são:

```text
ASPNETCORE_ENVIRONMENT
ConnectionStrings__LaresSql
Jwt__Chave
```

A connection string do Azure SQL e a chave JWT são armazenadas como **variáveis secretas no Azure DevOps**.

Durante a Release, esses valores são enviados ao Azure App Service por meio das **Application Settings**.

Dessa forma, credenciais e segredos permanecem fora do repositório.

---

## Testes e validação

A pipeline de CI executa automaticamente a suíte de testes xUnit antes da publicação da aplicação.

Para executar os testes localmente:

```bash
dotnet test
```

A solução também é validada após o deploy através de:

- Swagger/OpenAPI;
- Health Checks;
- testes de autenticação;
- operações CRUD;
- consultas `SELECT` diretamente no Azure SQL para comprovar a persistência.

O fluxo de deploy somente prossegue com a versão da aplicação compilada, testada e publicada como artefato pela pipeline de CI.