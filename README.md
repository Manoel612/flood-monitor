# Monitor de Enchentes — protótipo

Aplicativo web colaborativo para registrar enchentes (local, altura da água, descrição e foto, sem login)
e visualizar no mapa e em métricas as áreas de risco da cidade.

Projeto de extensão — CST em Análise e Desenvolvimento de Sistemas, UNINTER.
ODS 11 (Cidades e comunidades sustentáveis) e ODS 13 (Ação contra a mudança global do clima).

## Tecnologias

| Camada | Tecnologia |
|---|---|
| Backend | ASP.NET Core (.NET 10), controllers MVC, Entity Framework Core + SQLite |
| Frontend | Vue 3 + Vite, Vuetify 4 (Material Design), vue-router |
| Mapa | Leaflet + OpenStreetMap (gratuito, sem chave de API) |
| Gráficos | Chart.js (vue-chartjs) |
| Celular | PWA: pode ser instalado na tela inicial, sem loja de aplicativos |

## Arquitetura (MVC)

```
flood-monitor/
├── FloodMonitor.Server/              ASP.NET Core
│   ├── Models/                       M — entidades (Occurrence, Feedback, PageVisit) e DTOs
│   ├── Controllers/                  C — API REST
│   │   ├── OccurrencesController     registrar, listar e detalhar ocorrências
│   │   ├── MetricsController         resumo, por bairro, por mês, pontos críticos
│   │   ├── FeedbackController        avaliações anônimas do app
│   │   ├── EvidenceController        registros de acesso, resumo de uso e CSVs
│   │   ├── RainfallController        chuva por mês por cidade (seção Chuvas)
│   │   └── AdminController           login da área administrativa
│   ├── Services/                     fotos (PhotoStorage), geocodificação (GeocodingService), limites por IP
│   │   └── Rainfall/                 Open-Meteo, CEMADEN, municípios do IBGE e a sincronização em segundo plano
│   ├── Data/                         DbContext, consultas (OccurrenceQueries) e criação de tabelas novas (DatabaseSchema)
│   └── wwwroot/                      recebe o build do Vue (gerado, fora do git)
└── FloodMonitor.Client/              V — Vue 3 (SPA)
    └── src/
        ├── views/                    MapView, OccurrencesView, OccurrenceView, ReportView, MetricsView, RainfallView, FeedbackView, AboutView, AdminView
        ├── components/               BaseMap, WaterLevelPicker, LocationFilter, PeriodFilter, StatCard, StepCard…
        ├── services/api.js           chamadas à API
        └── utils/                    níveis da água (waterLevels), formatação (formatters), local, redução de fotos (image)
```

Fluxo: o Vue (View) chama a API → o Controller valida e usa os Services → o EF Core grava no SQLite (Model)
→ o Controller devolve JSON → o Vue atualiza a tela.

O código (nomes de classes, métodos, variáveis, arquivos, pastas, rotas da API e colunas do banco) está em
inglês. Os textos que aparecem para o usuário (tela, mensagens de erro e planilhas CSV) continuam em português.

## Identidade visual (Material Design 3)

| Papel | Claro | Escuro | Uso |
|---|---|---|---|
| primary / container | `#796C86` / `#EEE4F6` | `#D6C5E7` / `#554961` | ação principal, marca / item ativo do menu, seleção |
| secondary / container | `#74AA9B` / `#D3ECE4` | `#8FC5B5` / `#2E5148` | botões tonais (só ações, nunca seleção) |
| tertiary / container | `#91C68D` / `#DDF3D9` | `#A8DBA4` / `#2D5A2B` | confirmações e sucesso |
| warning | `#ECE488` | `#ECE488` | avisos |
| surface-variant (destaque) | `#F6F5CD` | `#37361F` | dicas, privacidade |

- A paleta fica em `src/plugins/palette.js`. Toda combinação de texto e fundo passa no WCAG AA (4,5:1) nos dois temas.
  Como `#74AA9B`, `#91C68D` e `#ECE488` são cores claras, o texto sobre elas é escuro.
- **Tema escuro:** segue o sistema por padrão; o botão de sol/lua na barra superior troca o tema e guarda a escolha no navegador.
  No escuro, os tiles do mapa também são escurecidos.
- **Botões:** use `<primary-button>` para a ação principal da tela (elevated + primary) e `<tonal-button>` para as
  secundárias (MD3 "filled tonal": secondary-container). Para ações discretas, `v-btn variant="text" color="primary"`.
- **Cards:** `rounded="lg"` e elevação 1. Elementos flutuantes sobre o mapa usam elevação 3. Botões e campos usam `rounded="md"`.
- **Layout:** conteúdo de até 1280px, centralizado (`.page-content`). O grid `.layout-grid` tem 4 colunas no celular e 12 a partir de 840px,
  com 16px de espaço (`density="comfortable"`). Seções empilhadas usam a classe `.section` (16px entre elas).
- **Navegação:** menu lateral temporário, aberto pelo ícone ☰ e fechado pelo X, tocando fora ou com Esc.
- **Níveis da água:** cor + texto + ícone de medidor (ponteiro vazio → cheio), para a gravidade não depender só da cor.
  Os marcadores do mapa têm borda na cor do texto do tema (escura no claro, clara no escuro) para contrastar com o mapa.
- **Fonte:** Roboto, instalada pelo `@fontsource/roboto` (funciona offline).

## Como rodar

Pré-requisitos: [.NET 10 SDK](https://dotnet.microsoft.com/download) e [Node.js](https://nodejs.org) 22 ou mais novo.

### Desenvolvimento (dois terminais)

Terminal 1 — API em http://localhost:5080:

```bash
cd FloodMonitor.Server
dotnet run
```

Terminal 2 — interface com recarga automática em http://localhost:5173:

```bash
cd FloodMonitor.Client
npm install
npm run dev
```

Abra http://localhost:5173. O Vite repassa `/api` e `/uploads` para o ASP.NET Core.

### Um único site (como em produção)

```bash
cd FloodMonitor.Client
npm run build
cd ../FloodMonitor.Server
dotnet run
```

Abra http://localhost:5080. O build do Vue vai para `FloodMonitor.Server/wwwroot` e o ASP.NET Core
serve tudo.

### Testar no celular

`npm run dev` mostra um endereço "Network" (ex.: `http://192.168.0.10:5173`) que abre no celular
conectado ao mesmo Wi-Fi. O navegador só libera o **GPS** em HTTPS: pelo endereço da rede local, o botão
"Usar minha localização" avisa e a pessoa marca o local tocando no mapa. Para testar o GPS e a instalação
do PWA, use um túnel HTTPS (ex.: Dev Tunnels do Visual Studio/VS Code ou `cloudflared`) ou publique em
uma hospedagem com HTTPS.

## Configuração

### Por ambiente

| Ambiente | Configurações (vão para o git) | Senhas e tokens (nunca vão para o git) |
|---|---|---|
| Desenvolvimento (`dotnet run`, Visual Studio) | `appsettings.json` + `appsettings.Development.json` | `.env.development` na raiz do projeto |
| Produção (Docker, Kubero) | `appsettings.json` + `appsettings.Production.json` | `.env.production` (`docker run --env-file .env.production ...`) ou o painel da hospedagem |

- O ambiente vem de `ASPNETCORE_ENVIRONMENT`. O `dotnet run` usa `Development` (`Properties/launchSettings.json`), e a
  imagem Docker usa `Production`.
- O servidor lê sozinho o `.env.{ambiente}` (`Services/EnvFile.cs`), procurando na pasta atual e nas de cima. Uma
  variável já definida no sistema ou no painel da hospedagem sempre vale mais que o arquivo, e linhas vazias depois
  do `=` são ignoradas. O log da inicialização mostra qual arquivo foi lido.
- Para criar os arquivos, copie o [`.env.example`](.env.example) para `.env.development` e `.env.production`. Use
  tokens diferentes em cada um.
- `appsettings.Development.json` mostra mais detalhes no log; `appsettings.Production.json` registra só avisos e erros.

### Chaves do `appsettings.json`


| Chave | Padrão | Para que serve |
|---|---|---|
| `Geocoding:Enabled` | `true` | Descobre o bairro pelo Nominatim (OpenStreetMap). Desative se estiver sem internet. |
| `Rainfall:Enabled` | `true` | Busca em segundo plano os dados da seção Chuvas. Desative se estiver sem internet. |
| `TimeZone` | `America/Sao_Paulo` | Fuso usado para agrupar métricas e evidências por dia e mês. |

### Variáveis de ambiente

Modelo em [`.env.example`](.env.example). Nunca coloque os valores no código, no `appsettings` ou no git.

| Variável | Para que serve |
|---|---|
| `Admin__Username` | Usuário da área administrativa. |
| `Admin__Password` | Senha da área administrativa (mínimo de 12 caracteres). |
| `Cemaden__Email` e `Cemaden__Password` | Conta gratuita da [Plataforma de Entrega de Dados do CEMADEN](https://ped.cemaden.gov.br). Liga as medições dos pluviômetros na seção Chuvas. Sem elas, a seção mostra só as estimativas do Open-Meteo. |
| `Admin__Token` | Token fixo entregue ao site depois do login e exigido nos relatórios e na exclusão (mínimo de 32 caracteres). Trocar o token encerra todas as sessões. Sem as três variáveis, a área administrativa fica desligada. |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | `true` quando o app fica atrás de um proxy que cuida do HTTPS (Render, Railway, nginx...), para o limite de envios usar o IP real de cada pessoa. |
| `AllowedHosts` | Domínio público do site (ex.: `monitor.exemplo.com.br`), para recusar requisições com outro endereço. |

Toda chave do `appsettings.json` também pode vir de variável de ambiente, trocando `:` por `__`
(ex.: `Geocoding__Enabled=false`).

`FloodMonitor.Client/src/config.js`: centro e zoom iniciais do mapa (troque para a sua cidade).

O banco SQLite e as fotos ficam em `FloodMonitor.Server/App_Data/`. Para começar do zero, pare o
servidor e apague essa pasta.

## API

| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/occurrences?from=&to=` | Ocorrências para o mapa (datas ISO, opcionais) |
| GET | `/api/occurrences/recent?page=1&pageSize=12&from=&to=&mine=` | Ocorrências completas (descrição e foto), da mais recente para a mais antiga, em páginas. Aceita o filtro de local; com `mine=true`, só as do aparelho do cabeçalho `X-Author` |
| GET | `/api/occurrences/places` | Estados, cidades e bairros que têm ocorrências, com o total de cada um (para os filtros) |
| GET | `/api/occurrences/{id}` | Detalhe de uma ocorrência |
| DELETE | `/api/occurrences/{id}` | Exclui a ocorrência e a foto dela (admin) |
| POST | `/api/occurrences` | Registrar (multipart: `latitude`, `longitude`, `waterLevel`, `description`, `occurredAt`, `photo`) |
| GET | `/api/metrics/summary?from=&to=` | Totais, bairro mais afetado e divisão por nível da água |
| GET | `/api/metrics/by-neighborhood?from=&to=` | Os 10 bairros com mais ocorrências |
| GET | `/api/metrics/by-month?months=12` | Total por mês, dividido por nível da água |
| GET | `/api/metrics/hotspots?from=&to=` | Locais (~110 m) que alagaram em 2 dias diferentes ou mais |
| POST | `/api/feedback` | Avaliação anônima (`rating` 1–5, `profile`, `comment`) |
| GET | `/api/rainfall/cities` | Cidades com ocorrências, com o centro dos registros (para a seção Chuvas) |
| GET | `/api/rainfall/monthly?city=&state=` | Chuva por mês nos últimos 12 meses (medida e estimada), pluviômetros da cidade e enchentes por mês |
| POST | `/api/admin/login` | Login da área administrativa (`username`, `password`); devolve o token |
| POST | `/api/evidence/visits` | Registro anônimo de visita (enviado pelo próprio app) |
| GET | `/api/evidence/summary` | Resumo de uso (acessos, visitantes, ocorrências, avaliações) (admin) |
| GET | `/api/evidence/{visits,occurrences,feedback}.csv` | Planilhas para o relatório (admin) |

**Filtro de local:** as rotas `/api/metrics/*` e `/api/occurrences/recent` aceitam `state=`, `city=` e
`neighborhood=` (combinados; os valores possíveis vêm de `/api/occurrences/places`) ou `lat=&lon=&radius=` (raio em
metros, de 100 a 5000; padrão 1000). Sem esses parâmetros, valem todos os lugares.

**"Enviadas por mim":** o navegador gera um código aleatório (guardado em `localStorage`) e o envia no
cabeçalho `X-Author` ao registrar e ao listar. O servidor guarda só o hash SHA-256 desse código
(`Occurrence.AuthorHash`), que nunca aparece nas respostas da API.

As rotas marcadas com "(admin)" exigem o cabeçalho `Authorization: Bearer <Admin__Token>`.

Em desenvolvimento, a especificação OpenAPI fica em http://localhost:5080/openapi/v1.json.

## Proteções (sem login)

- **Limite por IP:** 5 ocorrências e 3 avaliações a cada 10 minutos (`Services/IpRateLimits.cs`).
- **Campo-armadilha** invisível no formulário: robôs costumam preenchê-lo e o envio é recusado.
- **Fotos:** até 5 MB, só JPEG (conferido pelos bytes do arquivo), salvas com nome aleatório.
- **Privacidade:** a foto é reduzida no celular antes do envio, e o servidor apaga de novo os metadados
  (EXIF com GPS exato e modelo do aparelho, XMP, IPTC e comentários), mesmo de quem envia direto pela API.
  Os registros de acesso não guardam IP nem dados pessoais.
- **Área administrativa:** usuário, senha e token ficam só em variáveis de ambiente, nunca no código. O login
  aceita 5 tentativas a cada 10 minutos por IP, compara os valores em tempo constante e registra no log as
  tentativas que falham. O token fica no `sessionStorage` do navegador e some ao fechar a aba ou clicar em
  "Sair". Sem as variáveis configuradas, o login e as rotas de admin respondem 404.
- **Navegador:** em produção o servidor força HTTPS (redirecionamento e HSTS) e envia cabeçalhos de segurança:
  política de conteúdo (CSP) que só libera o próprio site e o OpenStreetMap, `X-Content-Type-Options`,
  bloqueio de abrir o site dentro de outro (`X-Frame-Options`), `Referrer-Policy` e `Permissions-Policy`
  (`Services/SecurityHeaders.cs`).
- **Moderação:** na área administrativa (ou no detalhe da ocorrência, logado como admin), o botão de lixeira
  apaga a ocorrência e a foto de vez, depois de confirmar. Para só esconder um registro sem apagá-lo, marque
  `IsHidden = 1` na tabela `Occurrences` (ex.: com o DB Browser for SQLite).

## Desenho Universal (os 7 princípios)

| Princípio | Como o protótipo atende |
|---|---|
| 1. Uso equitativo | Sem cadastro nem login; funciona em qualquer navegador de celular ou computador. |
| 2. Flexibilidade no uso | Local por GPS **ou** tocando no mapa; foto e descrição opcionais; data "agora" ou outro momento. |
| 3. Uso simples e intuitivo | Registro em 4 passos numerados, em linguagem simples, com o resultado confirmado na tela. |
| 4. Informação perceptível | Nível da água com ícone + texto + cor (a cor nunca aparece sozinha); gráfico com descrição para leitores de tela. |
| 5. Tolerância ao erro | Marcador arrastável para corrigir o local; aviso do que falta antes de enviar; mensagens de erro claras. |
| 6. Baixo esforço físico | Botões grandes (até 88 px), poucos campos; foto reduzida para gastar menos dados. |
| 7. Tamanho e espaço para uso | Layout pensado primeiro para o celular, com o botão "Registrar enchente" fixo ao alcance do polegar. |

## Seção Chuvas

A página **Chuvas** compara, mês a mês, a chuva de uma cidade com as enchentes registradas no app. Ela junta
três fontes públicas, sem misturar os números:

| Fonte | O que entra | Tipo |
|---|---|---|
| [CEMADEN](https://ped.cemaden.gov.br) | Chuva acumulada em 24 h de cada pluviômetro da cidade. O gráfico usa o pluviômetro mais próximo do centro das ocorrências. | Medição |
| [Open-Meteo](https://open-meteo.com) | Chuva diária estimada pelo modelo ERA5 para o centro das ocorrências | Estimativa |
| [IBGE](https://servicodados.ibge.gov.br/api/docs/localidades) | Código do município, que o CEMADEN exige nas consultas | Cadastro |

- O servidor busca os dados em segundo plano (`Services/Rainfall/RainfallSyncService.cs`) a cada 6 horas e guarda
  tudo no banco, nas tabelas `RainfallReadings` e `RainfallCheckedDays`. A página só lê o banco.
- Só entram as cidades que já têm ocorrências registradas. Quando uma cidade nova aparece, a busca começa na hora.
- O Open-Meteo é livre para uso não comercial, sem chave, e pede crédito (licença CC BY 4.0), que aparece na página.
  Os dados têm alguns dias de atraso.
- O CEMADEN exige conta (`Cemaden__Email` e `Cemaden__Password`) e aceita poucas consultas por minuto. O servidor faz
  uma consulta por dia e por cidade e respeita o limite, então o primeiro ano de uma cidade leva cerca de 40 minutos
  para carregar. Depois disso, só os dias novos são consultados.

## Evidências para a Atividade Extensionista

O app registra visitas anônimas e avaliações. Os relatórios ficam na **área administrativa**
(menu ☰ → Área administrativa, ou `/admin`): acessos e visitantes por dia, páginas mais visitadas,
avaliações por perfil, últimos comentários e o botão para baixar as planilhas CSV (acessos, ocorrências
e avaliações) para anexar ao relatório. As visitas à própria área administrativa não entram na contagem.

Para ativar o login, preencha `Admin__Username`, `Admin__Password` e `Admin__Token` no `.env.development`
(desenvolvimento) ou no `.env.production` / painel da hospedagem (produção) e reinicie o servidor.
O token pode ser gerado com:

```bash
node -e "console.log(require('crypto').randomBytes(32).toString('base64url'))"
```

Antes de aplicar com a comunidade:

1. Apague `FloodMonitor.Server/App_Data/` para remover registros de teste.
2. Publique com HTTPS (necessário para GPS e instalação no celular) e configure as variáveis de ambiente
   do `.env.example`.
3. Em oficinas presenciais com todos no mesmo Wi-Fi, as pessoas podem compartilhar o mesmo IP público:
   aumente temporariamente o limite de envios em `Services/IpRateLimits.cs`.

## Limitações conhecidas e próximos passos

- O banco é criado com `EnsureCreated()`. Num banco já existente, `Data/DatabaseSchema.cs` cria só as tabelas que
  faltam, mas não altera colunas de tabelas antigas. Antes de mudar colunas, vale passar para as migrations do EF Core.
- A integração com o CEMADEN segue a especificação pública da plataforma, mas não foi testada com uma conta real.
  Ao ativá-la, confira os primeiros números com o mapa interativo do CEMADEN. Duas suposições a conferir: o horário
  de referência das consultas está em UTC, e o campo `acc24hr` traz a chuva das 24 h anteriores em milímetros.
- A estimativa do Open-Meteo é para o centro das ocorrências da cidade, não para cada bairro.
- "Enviadas por mim" vale por aparelho e navegador: em outro celular, numa aba anônima ou depois de limpar os
  dados do navegador, a pessoa não vê os registros antigos. Não há como recuperar sem login.
- Estado, cidade e bairro vêm do OpenStreetMap no momento do registro; se a busca falhar, a ocorrência
  fica sem esses dados e não aparece nos filtros por lugar (só no filtro por endereço e raio).
- A busca de bairro é feita durante o envio; se o Nominatim estiver fora do ar, o envio espera até 5 s e a ocorrência fica sem bairro.
- A busca de endereço das Métricas consulta o Nominatim direto do navegador: o endereço digitado vai para o
  OpenStreetMap. A busca só acontece ao confirmar (a política do Nominatim não permite autocompletar).
- Os ícones vêm da fonte Material Design Icons (~400 KB). Trocar por ícones SVG (`@mdi/js`) reduz o download.
- Ainda não há testes automatizados.

## Créditos

Mapas © colaboradores do [OpenStreetMap](https://www.openstreetmap.org/copyright). Geocodificação:
[Nominatim](https://nominatim.org) (respeite a política de uso: no máximo 1 requisição por segundo).
Chuva estimada: [Open-Meteo.com](https://open-meteo.com/) (ERA5, CC BY 4.0). Chuva medida: rede de pluviômetros do
[CEMADEN](https://www.gov.br/cemaden). Códigos de municípios: [IBGE](https://www.ibge.gov.br).
