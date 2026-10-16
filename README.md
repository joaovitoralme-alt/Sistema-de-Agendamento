# Site + agendamento para clínica de estética

- **Backend/**: API em .NET 10 (C#), Clean Architecture, PostgreSQL
- **Frontend/**: site público + painel em Vue 3 + Tailwind

O site em Vue é compilado para `Backend/src/Clinica.Api/wwwroot`, então **uma única aplicação .NET serve tudo** (site, painel e API). Isso deixa a hospedagem mais simples e barata.

## Rodar agora (sem instalar banco)

Em modo Development a API usa um **banco em memória** com dados de exemplo (5 procedimentos, 2 profissionais). Os dados somem ao reiniciar.

```bash
cd Backend
dotnet run --project src/Clinica.Api
```

Abra http://localhost:5219

- Site: `/`
- Painel: `/admin`, entrar com **admin@clinica.local** / **trocar-esta-senha**

## Usuários do painel e histórico

- O login usa **JWT**: depois de entrar, o navegador guarda um token e envia em cada pedido (`Authorization: Bearer ...`).
- Cada usuário tem um **identificador** no formato `@a123456@`, gerado automaticamente. Dá para entrar com ele ou com o e-mail.
- Para cadastrar alguém: **Usuários › Novo usuário**. O sistema mostra um **código de acesso** (uma vez só, vale 7 dias).
  A pessoa abre `/admin/primeiro-acesso`, informa identificador + código e cria a própria senha.
- Esqueceu a senha? **Usuários ›** clique na pessoa **› Gerar novo acesso**.
- Cada um troca a própria senha em **Minha conta**. Trocar a senha ou desativar o usuário derruba as sessões abertas.
- Tudo o que é feito no painel fica registrado em **Histórico** (quem, o quê, quando), sem possibilidade de edição.

## Mexer no front-end com recarga automática

Com a API rodando, em outro terminal:

```bash
cd Frontend
npm install        # só na primeira vez
npm run dev        # abre em http://localhost:5173
```

Quando terminar, `npm run build` atualiza o `wwwroot` da API.

## Usar o PostgreSQL

1. Suba o banco: `cd Backend && docker compose up -d` (ou instale o PostgreSQL e ajuste a connection string)
2. Em `Backend/src/Clinica.Api/appsettings.Development.json`, troque `"Provedor": "Memoria"` por `"Provedor": "Postgres"`
3. Rode a API. As tabelas são criadas sozinhas na primeira execução.

## Testes

```bash
cd Backend
dotnet test
```

## Colocar no ar (produção)

Configure por variáveis de ambiente (nunca deixe senha no código):

| Variável | Exemplo |
|---|---|
| `ConnectionStrings__Clinica` | `Host=...;Database=clinica;Username=...;Password=...` |
| `Jwt__Chave` | segredo aleatório de 32+ caracteres que assina os tokens de login |
| `Admin__Email` | e-mail do primeiro administrador |
| `Admin__Senha` | senha inicial (mínimo 8 caracteres; troque depois no painel) |
| `Fotos__Pasta` | pasta persistente para as fotos enviadas |

Depois: `dotnet publish src/Clinica.Api -c Release` e suba a pasta gerada. Use HTTPS.

## Como o código está organizado

```
Backend/src/
  Clinica.Domain/          regras puras: entidades, Periodo, Disponibilidade
  Clinica.Application/     casos de uso: AgendamentoOnline, GestaoDeAgendamentos, Cadastros
  Clinica.Infrastructure/  PostgreSQL (EF Core), banco em memória, fotos, senha
  Clinica.Api/             controllers HTTP, login por cookie, limite de requisições
Frontend/src/
  api/                     única camada que faz chamadas HTTP
  dominio/                 formatos de data, status, sessão
  views/publico/           site e fluxo de agendamento
  views/admin/             painel da clínica
```

## Pontos para evoluir

- Trocar `EnsureCreated` por **Migrations** do EF Core antes de ter dados reais (permite alterar tabelas sem perder dados).
- A trava contra agendamento duplo funciona com um servidor só. Se um dia rodar em vários, mover a trava para o banco.
