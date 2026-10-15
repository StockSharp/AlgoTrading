# Estratégia CMO Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia é uma portagem do StockSharp do expert MetaTrader 5 `Exp_CMO_Duplex.mq5`. Ela divide a lógica em duas pernas independentes
(longa e curta) que ambas reagem a cruzamentos da linha zero do Oscilador de Momentum Chande (CMO). Cada perna pode consumir sua própria
série de velas, período e deslocamento de sinal, o que torna possível executar configurações assimétricas no mesmo instrumento.

## Como funciona

- A estratégia subscreve um ou dois feeds de velas dependendo de se as pernas longa e curta usam o mesmo `DataType`.
- Cada perna possui sua própria instância do indicador CMO. O indicador é avaliado apenas em velas concluídas.
- A configuração `SignalBar` define quantas velas concluídas atrás no histórico devem ser usadas para a lógica de cruzamento. Um valor de 0
  significa «usar a barra fechada mais recente», `1` usa a barra anterior, `2` usa a barra antes dessa, e assim por diante.
- **Perna longa:** quando o valor CMO selecionado cruza de acima de zero para zero ou abaixo, a estratégia entra (ou muda para) uma posição
  longa se as entradas longas são permitidas. Saídas longas são acionadas quando o valor mais antigo do CMO está abaixo de zero ou quando
  os níveis de stop loss / take profit são tocados.
- **Perna curta:** espelha a lógica longa. Um cruzamento de abaixo de zero para zero ou acima abre (ou muda para) uma posição curta e
  o sinal oposto do valor CMO ou os stops configurados fecham a posição.
- Mudanças de posição reutilizam `Volume` mais qualquer exposição oposta, portanto uma única ordem a mercado fecha a posição anterior e
  abre a nova.
- `StartProtection()` é habilitado na inicialização, para que os controles de risco integrados do StockSharp permaneçam ativos.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `LongCandleType` | Tipo de vela usado pela perna longa. |
| `LongCmoPeriod` | Período do indicador CMO no lado longo. |
| `LongSignalBar` | Número de barras fechadas entre o tempo atual e a barra analisada para sinais (0 = barra fechada mais recente). |
| `EnableLongEntries` | Permite ou bloqueia a abertura de novas posições longas. |
| `EnableLongExits` | Permite ou bloqueia o fechamento de posições longas em sinais do oscilador. |
| `LongStopLossPoints` | Distância do stop-loss em passos de preço para operações longas (0 desabilita o stop). |
| `LongTakeProfitPoints` | Distância do take-profit em passos de preço para operações longas (0 desabilita o alvo). |
| `ShortCandleType` | Tipo de vela usado pela perna curta. |
| `ShortCmoPeriod` | Período do indicador CMO no lado curto. |
| `ShortSignalBar` | Número de barras fechadas entre o tempo atual e a barra analisada para sinais curtos. |
| `EnableShortEntries` | Permite ou bloqueia a abertura de novas posições curtas. |
| `EnableShortExits` | Permite ou bloqueia o fechamento de posições curtas em sinais do oscilador. |
| `ShortStopLossPoints` | Distância do stop-loss em passos de preço para operações curtas (0 desabilita o stop). |
| `ShortTakeProfitPoints` | Distância do take-profit em passos de preço para operações curtas (0 desabilita o alvo). |

A propriedade base `Strategy.Volume` controla o tamanho padrão da ordem. Quando a estratégia precisa mudar de direção, ela envia uma ordem
a mercado cujo volume é igual a `Volume + |Position|`, o que fecha a exposição antiga e abre a nova em uma única transação.

## Gestão de riscos

- Os níveis de stop-loss e take-profit são avaliados em cada vela concluída. Para posições longas o stop é colocado abaixo da entrada
  e o alvo acima; para posições curtas os níveis são espelhados.
- Um stop ou um alvo aciona uma ordem a mercado imediata para fechar a posição. A mesma rotina de saída também é executada quando o valor
  do oscilador respectivo mantém o sinal errado (abaixo de zero para longos, acima de zero para curtos).
- Definir a distância como zero desabilita a proteção correspondente e deixa a perna gerenciada puramente pela lógica do oscilador.

## Notas de uso

- A estratégia funciona melhor em instrumentos onde o CMO tende a reverter após tocar a linha zero. Entradas contrárias são
  deliberadamente atrasadas pelo deslocamento `SignalBar` para corresponder ao expert original.
- As pernas longa e curta podem compartilhar o mesmo feed de velas ou operar em diferentes períodos. Se ambas usarem o mesmo `DataType`, a
  estratégia reutiliza uma única subscrição para melhor desempenho.
- Como a estratégia opera em velas concluídas, recomenda-se fornecer um fluxo contínuo de velas (por exemplo, via
  backtest histórico ou feed em tempo real) para evitar sinais perdidos.
