# Plano de implementação — Login e papéis

**Status:** aguardando aprovação (não iniciar código até revisão deste documento)  
**Data:** 2026-09-20  
**Objetivo:** autenticar pessoas no Gestoque, com **admin geral (SuperAdmin)** e **usuários comuns** ligados a uma empresa.

Marque os checkboxes ao concluir cada task.

---

## 1. Contexto atual (análise)

Hoje **não há login**. Qualquer um com a URL da Web opera o sistema. `ICurrentTenantService.IsSuperAdmin` está **hardcoded `true`**. O layout mostra badge SUPER ADMIN e o dropdown de todas as empresas. A API aceita qualquer `X-Tenant-Id` válido (ADR-002). `CreatedBy` nas movimentações não tem usuário real.

A Web é **Blazor Server in-process** (ADR-006): cookie de autenticação no circuito Blazor é o caminho natural. A API Minimal precisa de **JWT** para o mesmo modelo de papéis (já previsto no ADR-002).

Referências: [[01-Projeto/Multi-Tenancy]], [[01-Projeto/Interface-Web]], [[01-Projeto/CQRS-MediatR]], [[05-Decisoes/ADRs]].

---

## 2. Decisões propostas (v1) — validar antes de codar

Estas decisões entram num **ADR-009** na fase 0. Se discordar, ajuste aqui antes de implementar.

| Tema | Proposta v1 |
| --- | --- |
| Identidade | Entidade `User` no Domain + hash de senha no Infrastructure (`PasswordHasher<T>`). **Não** ASP.NET Identity completo (evita DbContext paralelo e tabelas extras). |
| Sessão Web | Cookie Authentication (`Gestoque.Web`), HTTPS em produção, cookie HttpOnly. |
| Sessão API | JWT Bearer (`Gestoque.Api`), claims `sub`, `role`, `tenant_id`. |
| Admin geral | Papel `SuperAdmin`. `TenantId` nulo. Vê todas as empresas, troca empresa ativa, cadastra empresas e usuários. |
| Usuário comum | Papel `Operator`. **Uma** empresa (`TenantId` obrigatório). Não troca de empresa. Não acessa `/empresas` nem `/importar-excel`. |
| Papel extra (adiado) | `TenantAdmin` (gerir usuários só da própria empresa) fica **fora da v1** — fase 7 opcional. |
| Primeiro acesso | Migrator cria SuperAdmin se não existir nenhum usuário. Credenciais via `.env` (`ADMIN_EMAIL`, `ADMIN_PASSWORD`) — nunca commitadas. |
| Tenant na API | Depois do login, o tenant vem do **token**, não de um header solto. SuperAdmin pode enviar `X-Tenant-Id` para agir em nome de uma empresa. Usuário comum: header ignorado ou rejeitado se divergir do token. |
| Auto-cadastro | **Não.** Só SuperAdmin cria usuários na v1. |
| Recuperar senha / 2FA / OAuth | **Fora da v1.** |

### O que o Operator pode (v1)

Posição de estoque, entradas, saídas, doações, movimentações, produtos, fornecedores, dashboard da **sua** empresa.

### O que só SuperAdmin pode (v1)

`/empresas` (criar/excluir tenant), `/importar-excel`, criar/inativar usuários, badge e dropdown de empresa.

---

## 3. Modelo de dados proposto

```
User
  Id, Email (unique), PasswordHash, DisplayName
  Role (enum: SuperAdmin | Operator)
  TenantId (Guid?, null se SuperAdmin)
  IsActive
  CreatedAt, UpdatedAt, LastLoginAt?

  → SuperAdmin: TenantId null
  → Operator: TenantId FK → Tenant
```

Índice único em `Email` (normalizado lowercase). Query filter de tenant **não** se aplica a `User` da mesma forma que a Product (SuperAdmin precisa listar todos). Handlers de usuário filtram explicitamente.

`ICurrentUserService` (novo) além de `ICurrentTenantService`:

- `UserId`, `Email`, `Role`
- `SetTenantId` só permitido se SuperAdmin (ou igual ao tenant do Operator)

Hoje `IsSuperAdmin = true` por padrão some: default **false** até o cookie/JWT preencher.

---

## 4. Fases e tasks

### Fase 0 — Aprovação e ADR

**Meta:** congelar regras antes de migration.

- [ ] Revisar e aprovar este plano (papéis v1, cookie + JWT, sem auto-cadastro)
- [ ] Escrever [[05-Decisoes/ADRs|ADR-009]] (autenticação Cookie Web + JWT API; SuperAdmin vs Operator)
- [ ] Atualizar ADR-002: header `X-Tenant-Id` deixa de ser a única “auth”
- [ ] Definir e-mail do SuperAdmin inicial (valor só no `.env`, documentar em `.env.example` sem senha real)
- [ ] Mover este plano para [[Em-Progresso]] quando o código começar

**Saída:** ADR aceito; este arquivo continua como checklist.

---

### Fase 1 — Domínio e persistência

**Meta:** tabela de usuários e seed do admin, sem tela ainda.

- [ ] Enum `UserRole` (`SuperAdmin`, `Operator`) em Domain
- [ ] Entidade `User` + configuração EF (`GestoqueDbContext`, `IGestoqueDbContext`)
- [ ] Migration nova (Migrator aplica; Api/Web sem DDL)
- [ ] `PasswordHasher` no Infrastructure (nunca logar senha)
- [ ] Seed no Migrator: cria SuperAdmin se `Users` vazio, lendo `ADMIN_EMAIL` / `ADMIN_PASSWORD`
- [ ] `.env.example` com as chaves (valores placeholder)
- [ ] Falhar o Migrator se seed necessário e senha/e-mail ausentes
- [ ] Teste: seed cria um SuperAdmin; hash não é texto puro

**Saída:** banco sobe com um admin; ainda dá para usar o sistema sem login (comportamento atual).

---

### Fase 2 — Application (casos de uso)

**Meta:** login e “quem sou eu” via MediatR, testável sem UI.

- [ ] `ICurrentUserService` (UserId, Role, Email)
- [ ] Ligar `ICurrentTenantService` ao usuário autenticado (Operator fixa tenant; SuperAdmin escolhe)
- [ ] Default `IsSuperAdmin = false`
- [ ] `LoginCommand` (email + senha) → resultado com user id / role / tenant (ou erro genérico “credenciais inválidas”)
- [ ] `GetCurrentUserQuery`
- [ ] `CreateUserCommand` (só SuperAdmin; Operator exige TenantId)
- [ ] `ListUsersQuery` (SuperAdmin: todos; não expor hash)
- [ ] `SetUserActiveCommand` (inativar; não deletar)
- [ ] `ChangePasswordCommand` (próprio usuário; SuperAdmin pode resetar)
- [ ] Proteção: não inativar o último SuperAdmin ativo
- [ ] Testes: login ok/senha errada; Operator não cria usuário; SuperAdmin cria Operator de um tenant

**Saída:** handlers prontos; UI e API ainda abertas.

---

### Fase 3 — Login na Web (cookie)

**Meta:** ninguém usa o sistema sem autenticar.

- [ ] `AddAuthentication` + Cookie no `Gestoque.Web`
- [ ] Página `/login` (layout limpo, **sem** menu/dropdown de empresas)
- [ ] SignInAsync com claims (`NameIdentifier`, `Role`, `tenant_id` se Operator)
- [ ] Redirect pós-login para `/`
- [ ] Logout no header (encerra cookie)
- [ ] `CascadingAuthenticationState` / `AuthorizeRouteView` em `Routes.razor`
- [ ] `[Authorize]` no layout autenticado; `/login` anônimo
- [ ] Middleware/circuito: preencher `ICurrentUserService` + tenant a partir das claims (não só localStorage)
- [ ] Operator: dropdown de empresa **oculto** (mostra só o nome da empresa)
- [ ] SuperAdmin: dropdown permanece
- [ ] Badge SUPER ADMIN só se o papel for SuperAdmin
- [ ] Parar de persistir tenant no browser como fonte de verdade para Operator (localStorage vira no máximo preferência do SuperAdmin)
- [ ] Teste manual: sem cookie → `/login`; senha errada não vaza se o e-mail existe

**Saída:** Web exige login. SuperAdmin e Operator já se comportam diferente no header.

---

### Fase 4 — Autorização de telas e comandos

**Meta:** menu e handlers respeitam papel.

- [ ] Políticas `SuperAdminOnly` e `Authenticated`
- [ ] Esconder no `NavMenu`: Empresas e Importar Excel para Operator
- [ ] Bloquear rotas `/empresas` e `/importar-excel` (não só esconder o link)
- [ ] `DeleteTenantCommand` / criar empresa: recusar se não SuperAdmin
- [ ] Import Excel: recusar se não SuperAdmin
- [ ] Operator: `GetTenantsQuery` retorna **só** a própria empresa (ou nem é chamado)
- [ ] Preencher `CreatedBy` com e-mail ou user id nas movimentações/doações
- [ ] Página simples “Acesso negado”

**Saída:** Operator isolado na própria empresa; admin geral mantém o poder atual.

---

### Fase 5 — Gestão de usuários (UI SuperAdmin)

**Meta:** criar logins comuns sem mexer no banco.

- [ ] Página `/usuarios` (só SuperAdmin)
- [ ] Lista: nome, e-mail, papel, empresa, ativo
- [ ] Formulário: criar Operator (empresa obrigatória) ou outro SuperAdmin
- [ ] Inativar / reativar
- [ ] Reset de senha (senha temporária ou campo “nova senha”)
- [ ] Validar e-mail único na UI com erro claro
- [ ] Item no menu Administração

**Saída:** operação do dia a dia sem seed manual.

---

### Fase 6 — API com JWT

**Meta:** a API deixa de confiar só no header.

- [ ] Endpoint `POST /api/v1/auth/login` (e-mail/senha → access token)
- [ ] JWT: `sub`, `role`, `tenant_id` (omitir tenant se SuperAdmin)
- [ ] `AddAuthentication().AddJwtBearer` na Api
- [ ] Middleware atual: tenant do token; SuperAdmin ainda pode mandar `X-Tenant-Id`
- [ ] Operator com `X-Tenant-Id` diferente do token → 403
- [ ] Sem token em rotas de dados → 401 (`/health` e `POST /auth/login` públicos)
- [ ] Tempo de expiração do token em config (ex.: 8h dev)
- [ ] Documentar exemplos em [[04-API/Endpoints]] (sem senhas reais)
- [ ] Teste de integração: login + GET estoque; GET estoque sem token falha

**Saída:** API alinhada à Web. curl precisa de Bearer.

---

### Fase 7 — Endurecimento (depois da v1 estável)

Não bloqueia o go-live interno se a fase 4 estiver ok.

- [ ] Lockout após N senhas erradas
- [ ] Política de senha (tamanho mínimo, não igual ao e-mail)
- [ ] HTTPS obrigatório / Secure cookie em produção
- [ ] Refresh token ou sliding expiration do cookie
- [ ] Papel `TenantAdmin` (usuários só da própria empresa)
- [ ] Usuário em **várias** empresas (tabela `UserTenant`) — só se o negócio pedir
- [ ] Recuperação de senha por e-mail
- [ ] Auditoria: último login, IP opcional
- [ ] Rotacionar JWT signing key via env

---

## 5. Ordem de entrega sugerida

```
Fase 0 (aprovação)
  → 1 (tabela + seed)
  → 2 (handlers + testes)
  → 3 (tela login + cookie)     ← primeiro valor visível
  → 4 (autorização)
  → 5 (CRUD usuários)
  → 6 (JWT na API)
  → 7 (hardening)
```

Não pular 3 sem 1–2 (senão a tela não tem de onde autenticar). A fase 6 pode paralelo à 5 depois da 4.

---

## 6. Riscos e cuidado

| Risco | Mitigação |
| --- | --- |
| Seed com senha fraca commitada | Só `.env`; Migrator recusa seed vazio |
| SuperAdmin preso sem acesso | Não inativar o último SuperAdmin; senha inicial anotada fora do git |
| Operator muda `localStorage` e troca tenant | Tenant do Operator vem só das claims, não do storage |
| API antiga com header continua aberta | Fase 6 obriga JWT; até lá a Web já está fechada (fase 3) |
| Blazor Server + cookie | Mesmo site, SameOrigin; não colocar JWT no JS |
| Query filter em User | Não filtrar Users pelo tenant global da mesma forma que Product |

---

## 7. Fora de escopo (v1)

- Cadastro público / convite por link
- Login Google/Microsoft
- 2FA
- Permissão por tela fina (ex.: “só consulta, sem saída”) — Operator é pacote fechado
- Múltiplas empresas por usuário

---

## 8. Como usar este arquivo

1. Comentar o que mudar nas decisões da seção 2.
2. Quando autorizar o código: copiar o título para [[Em-Progresso]] e ir marcando `[x]`.
3. Ao fechar uma fase, uma linha em [[Concluido]] e, se couber, atualizar [[03-Dominio/Entidades]] e [[04-API/Endpoints]].
