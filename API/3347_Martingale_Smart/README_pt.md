# Martingale Estratégia Inteligente
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Martingale Smart é uma conversão do consultor especialista MetaTrader "Martingale Smart". A estratégia mantém apenas uma posição aberta por vez e alterna entre dois filtros de entrada diferentes após cada ciclo de perda:

1. **Filtro primário** – cruzamento entre duas médias móveis simples combinadas com a direção de um histograma de período de tempo superior MACD. Este é o modo de entrada padrão.
2. **Filtro secundário** – envelopes de média móvel. Quando a perda flutuante do ciclo anterior é negativa a estratégia alterna para este filtro. Outra perda volta para o filtro primário.

O componente martingale aumenta o volume da próxima negociação após um ciclo de perdas. Você pode multiplicar o último volume (martingale clássico) ou adicionar um incremento fixo.

## Assinaturas de dados

* `CandleType` – prazo utilizado para os principais cálculos e gerenciamento de negociações.
* `MacdTimeFrame` – período secundário dedicado ao filtro MACD. O padrão é um mês para imitar o EA original que usou o período `PERIOD_MN1`.

Ambas as assinaturas são iniciadas automaticamente em `OnStarted`.

## Lógica de negociação

1. Uma nova negociação é considerada somente se não houver posição aberta e todos os indicadores estiverem formados.
2. O filtro primário fica comprado quando o MA rápido está abaixo do MA lento e a linha MACD está acima do sinal (mesma lógica para casos de baixa). Essas condições seguem o EA original que usava `iMA` e `iMACD` com um deslocamento de uma barra.
3. O filtro secundário usa um envelope de média móvel simples. Um fechamento acima da banda inferior sinaliza uma entrada longa, enquanto um fechamento abaixo da banda superior sinaliza uma entrada curta. Isso reproduz a lógica baseada em `iEnvelopes`.
4. Quando um ciclo termina com lucro negativo, a estratégia muda para o filtro alternativo e calcula o próximo volume de acordo com os parâmetros de martingale. Um ciclo rentável mantém o filtro atual e redefine o volume para o valor inicial.
5. Os níveis protetores de stop-loss e take-profit são anexados imediatamente após cada entrada usando distâncias baseadas em pip.

## Gestão de risco

* **Parada de equilíbrio** – quando o lucro não realizado atinge `BreakEvenTriggerPips`, o stop loss salta para o preço de entrada mais uma compensação opcional.
* **Trailing stop clássico** – mantém um stop móvel que fica a `TrailingStopPips` de distância do último fechamento.
* **Realizar lucro em dinheiro** – fecha a posição quando o lucro flutuante excede `MoneyTakeProfit`.
* **Realização de lucro em porcentagem** – semelhante à meta monetária, mas expressa como uma porcentagem do valor atual do portfólio.
* **Money trailing stop** – é ativado quando o lucro flutuante atinge `MoneyTrailingTarget`; posteriormente, a estratégia acompanha o pico de lucro e liquida a posição quando o rebaixamento excede `MoneyTrailingDrawdown`.

Todos os cálculos monetários dependem de `PriceStep` e `StepPrice` do instrumento. Se a fonte de dados não os fornecer, a estratégia recorre a uma simples estimativa de preço x volume.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `UseMoneyTakeProfit` | Ative a regra de lucro monetário fixo. |
| `MoneyTakeProfit` | Meta de lucro flutuante na moeda da conta. |
| `UsePercentTakeProfit` | Habilite o lucro com base em porcentagem. |
| `PercentTakeProfit` | Meta de lucro flutuante como% do valor do portfólio. |
| `EnableMoneyTrailing` | Habilite o lucro em dinheiro. |
| `MoneyTrailingTarget` | Nível de lucro que habilita o bloco final. |
| `MoneyTrailingDrawdown` | Retorno máximo de lucro permitido quando o trailing estiver ativo. |
| `UseBreakEven` | Mova o stop-loss para o ponto de equilíbrio após a distância configurada. |
| `BreakEvenTriggerPips` | Distância de lucro em pips necessária antes do movimento do stop. |
| `BreakEvenOffsetPips` | Pips adicionais adicionados ao ponto de equilíbrio. |
| `MartingaleMultiplier` | Fator de multiplicação aplicado após um ciclo perdedor. |
| `InitialVolume` | Volume utilizado para a primeira ordem de cada ciclo. |
| `UseDoubleVolume` | Se for verdade, multiplique o volume; caso contrário, aplique `LotIncrement`. |
| `LotIncrement` | Incremento de lote fixo usado quando a duplicação está desativada. |
| `TrailingStopPips` | Distância do trailing stop clássico em pips. |
| `StopLossPips` | Distância inicial do stop-loss em pips. |
| `TakeProfitPips` | Distância inicial de lucro em pips. |
| `FastMaPeriod` | Período da média móvel rápida. |
| `SlowMaPeriod` | Período da média móvel lenta. |
| `EnvelopePeriod` | Período da média móvel do envelope. |
| `EnvelopeDeviation` | Largura do envelope em porcentagem. |
| `MacdFastLength` | Comprimento EMA rápido dentro de MACD. |
| `MacdSlowLength` | Comprimento EMA lento dentro do MACD. |
| `MacdSignalLength` | Comprimento do sinal EMA dentro de MACD. |
| `CandleType` | Prazo do sinal principal. |
| `MacdTimeFrame` | Prazo para as velas MACD. |

## Notas de uso

1. O passo martingale é executado somente quando a posição anterior foi completamente fechada com perda.
2. A estratégia espera uma posição aberta de cada vez; sempre liquida a posição atual antes de entrar na direção oposta.
3. Para limites monetários precisos, configure as especificações do contrato do instrumento (`PriceStep`, `StepPrice` e `VolumeStep`).
4. O ponto de equilíbrio e os trailing stops são avaliados em velas fechadas no período principal; picos intrabarras são ignorados.

## Diferenças versus MetaTrader EA

* A conversão usa o API de alto nível de StockSharp (`SubscribeCandles` + `Bind`) e o indicador `MovingAverageConvergenceDivergenceSignal` em vez de chamadas diretas para `iMACD`.
* Algumas verificações específicas do corretor (níveis de congelamento, chamadas manuais de e-mail/notificação, loops baseados em tickets) são omitidas porque o mecanismo StockSharp gerencia esses aspectos internamente.
* As proteções baseadas em dinheiro operam em posições agregadas, em vez de cálculos por ticket, alinhando-se ao modelo de conta de StockSharp.
