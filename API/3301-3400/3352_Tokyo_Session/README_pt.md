# Estratégia da Sessão de Tóquio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A Estratégia da Sessão de Tóquio replica a lógica do consultor especialista MetaTrader *TokyoSessionEA_v2.8* em StockSharp. O
A estratégia foi projetada para rompimento intradiário ou negociação de reversão à média em torno da sessão asiática (Tóquio). Ele captura um
referencia a vela em uma hora configurável, cria um canal de preço a partir dessa vela e avalia o rompimento ou recuperação
condições em outra hora alvo. Dependendo do modo de sinal escolhido, a estratégia pode negociar contrariamente ao
quebra de nível (movimentos de fade que se estendem além da faixa de referência) ou ao longo da direção de fuga.

A porta StockSharp concentra-se no uso de API de alto nível. Todos os cálculos de sinal são realizados dentro da assinatura da vela
manipulador, as paradas são gerenciadas por meio de `StartProtection` e cada ação é registrada por meio de `LogInfo` para manter o comportamento
transparente durante backtests e negociações ao vivo.

## Lógica de negociação

1. **Vela de referência** – em `TimeSetLevels` (hora da corretora) a estratégia registra a máxima, a mínima e o fechamento da vela. Estes
os valores definem o canal da sessão e redefinem os sinalizadores de validação internos.
2. **Validação de canal** – cada vela finalizada entre a hora de referência e a hora de entrada pode invalidar o
sinal pendente dependendo da configuração:
   - `CheckAllBars`: se habilitado, o fechamento deve permanecer entre o máximo e o mínimo capturados.
   - `ReCheckPrices`: em `TimeRecheckPrices` o fechamento da vela é comparado com a média corrente para confirmar o impulso.
3. **Avaliação de entrada** – quando a vela que precede `TimeCheckLevels` fecha, a estratégia compara seu preço de fechamento
com as bordas do canal. Se o fechamento estiver dentro da faixa de distância configurada, a posição correspondente será aberta.
4. **Saídas** – as posições podem ser fechadas por três mecanismos:
   - `CloseInSignal` fecha uma negociação assim que o preço retorna dentro do canal (a lógica reflete o EA original).
   - `CloseOrdersOnTime` se estabiliza em `TimeCloseOrders` para evitar manter o risco na próxima sessão.
   - Stops de proteção, trailing stops e tratamento de ponto de equilíbrio são delegados ao subsistema de proteção StockSharp.

## Parâmetros

### Geral

| Parâmetro | Descrição |
|-----------|-------------|
| `CandleType` | Série de velas usada para análise (o padrão é H1). |
| `BrokerOffset` | Diferença entre o horário do corretor e o GMT em horas. |

### Sinais

| Parâmetro | Descrição |
|-----------|-------------|
| `TypeOfSignals` | `ContraryTrend` replica o esmaecimento do rompimento; `AccordingTrend` segue a direção do rompimento. |
| `TimeSetLevels` | Hora (0–23) em que a vela de referência é capturada. |
| `TimeCheckLevels` | Hora em que as condições de fuga são avaliadas. |
| `TimeRecheckPrices` | Hora adicional de verificação de impulso. |
| `MinDistanceOfLevel` | Distância mínima (em pips) entre o fechamento e o canal antes de permitir uma negociação. |
| `MaxDistanceOfLevel` | Distância máxima (em pips) do nível. Zero desativa o limite. |
| `ReCheckPrices` | Ativa/desativa o filtro de impulso adicional. |
| `CheckAllBars` | Requer que todos os fechamentos intermediários permaneçam dentro do canal. |

### Gestão de risco

| Parâmetro | Descrição |
|-----------|-------------|
| `CloseInSignal` | Saia assim que o preço cruzar o limite do canal. |
| `CloseOrdersOnTime` | Achate posições após `TimeCloseOrders`. |
| `TimeCloseOrders` | Hora usada pela saída baseada em tempo. |
| `UseTakeProfit`, `TakeProfit` | Habilite e configure um take-profit fixo (pips). |
| `UseStopLoss`, `StopLoss` | Habilite e configure um stop-loss de proteção (pips). |
| `UseTrailingStop`, `TrailingStop`, `TrailingStep` | Ative o gerenciamento de trailing stop StockSharp (pips). |
| `UseBreakEven`, `BreakEven`, `BreakEvenAfter` | Mova o stop loss para o ponto de equilíbrio quando o lucro atingir a distância de gatilho. |

### Negociação

| Parâmetro | Descrição |
|-----------|-------------|
| `Volume` | Volume básico do pedido. Ao inverter a direção, a posição oposta é fechada automaticamente. |
| `MaxOrders` | Número máximo de `Volume` blocos permitidos em uma direção. Defina como 0 para não ter limite. |

## Fluxo de trabalho

1. Implemente a estratégia em um instrumento com uma etapa de preço válida (`Security.PriceStep`).
2. Selecione o período desejado e configure as compensações de horário da corretora para alinhar a programação diária com a bolsa.
3. Ajuste os filtros de distância e validação para corresponder ao comportamento do EA original ou para se adaptar a diferentes mercados.
4. Configure parâmetros de risco. A porta StockSharp gerencia nativamente paradas e lógica de trilha por meio de `StartProtection`.
5. Comece a estratégia. As mensagens de registro reportarão os níveis capturados, negociações abertas e decisões de saída.

## Diferenças da versão MetaTrader

- Entradas de ponto flutuante baseadas em `UseFloatingPoint` e `PipsFloatingPoint` não são implementadas porque StockSharp
executa ordens de mercado no momento em que o sinal é gerado.
- Os filtros de spread e slippage são omitidos porque as assinaturas de velas de alto nível não fornecem dados de compra/venda em nível de tick.
- O gerenciamento automático de dinheiro (`AutoLotSize`, `RiskFactor`, lotes de recuperação, troca de símbolos predefinidos) é substituído pelo
parâmetros `Volume` e `MaxOrders` mais simples. O dimensionamento da posição deve ser ajustado diretamente nas configurações da estratégia.
- Notificações sonoras e impressas são substituídas por mensagens `LogInfo`.

Todas as outras condições de sinal, portas de validação e saídas baseadas em tempo refletem o comportamento do EA original.

## Notas

- A configuração padrão está alinhada com o período H1 recomendado pelo consultor especialista original. Outros prazos
pode ser usado, mas a lógica baseada em horas assume que as durações das velas dividem uma hora igualmente.
- Certifique-se de que o feed de dados forneça velas contínuas para o período selecionado. A falta de velas pode invalidar o
verificações de média e canal.
- Como a estratégia fecha posições enviando ordens de mercado, as corretoras que exigem ordens limitadas ou detenção mínima
vezes podem precisar de adaptações adicionais.
