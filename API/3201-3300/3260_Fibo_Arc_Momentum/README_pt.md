# Estratégia de Fibo Arc Momentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia é um port StockSharp do consultor especialista MetaTrader "FiboArc" (pasta `MQL/24924`). O EA original combina múltiplos filtros de Momentum com rompimentos de arcos de Fibonacci. A implementação StockSharp mantém a mesma ideia adaptando-a à API de velas de alto nível:

* Duas médias móvias ponderadas lineares (`FastMaPeriod`, `SlowMaPeriod`) definem a direção da tendência.
* Um oscilador de Momentum medido contra o nível neutro de 100 filtra configurações fracas.
* Um histograma MACD confirma a força da tendência e detecta cruzamentos frescos.
* Um arco de Fibonacci simplificado é reconstruído em cada barra usando os preços de abertura de duas velas âncora selecionadas por `TrendAnchorLength` e `ArcAnchorLength`. Um rompimento através deste nível dinâmico substitui as verificações baseadas em objetos da versão MetaTrader.

A estratégia funciona com qualquer par símbolo/período suportado pelo StockSharp. Todos os cálculos são executados em velas completamente concluídas para espelhar o comportamento do EA e evitar viés de antecipação.

## Indicadores e fluxo de dados

A estratégia assina um único fluxo de velas configurado por `CandleType`. Cada nova vela concluída é alimentada nos seguintes indicadores via `SubscribeCandles(...).BindEx(...)`:

| Indicador | Propósito | Configurações padrão |
|-----------|---------|------------------|
| LinearWeightedMovingAverage (rápida) | Tendência de curto prazo e timing de entrada | `FastMaPeriod = 6`, preço típico |
| LinearWeightedMovingAverage (lenta) | Filtro de tendência de nível superior | `SlowMaPeriod = 85`, preço típico |
| Momentum | Distância de 100 é usada para confirmar movimentos fortes | `MomentumPeriod = 14` |
| MovingAverageConvergenceDivergenceSignal | Confirma a tendência e detecta cruzamentos | `MacdFastPeriod = 12`, `MacdSlowPeriod = 26`, `MacdSignalPeriod = 9` |

As saídas dos indicadores são recebidas como instâncias `IIndicatorValue`; apenas valores finais são processados.

## Reconstrução do arco de Fibonacci

MetaTrader desenha um objeto de arco real e lê seus valores com `ObjectGetValueByShift`. O StockSharp não depende de objetos de gráfico, então o arco é emulado numericamente:

1. A estratégia mantém uma lista contínua de velas concluídas (`_history`).
2. `TrendAnchorLength` seleciona o índice da âncora base, e `ArcAnchorLength` seleciona a segunda âncora.
3. O nível do arco para a vela atual é calculado como uma interpolação linear entre as aberturas das âncoras usando `FibonacciRatio` (padrão 0.618).
4. Para detecção de rompimentos, compara-se a abertura da vela anterior com o nível de arco anterior e a abertura da vela atual com o nível recém-calculado. Um cruzamento de baixo para cima (`fibCrossUp`) ou de cima para baixo (`fibCrossDown`) recria as verificações originais do EA.

## Regras de trading

### Entradas longas

Uma posição comprada é aberta quando todas as condições abaixo são satisfeitas:

1. A barra anterior abriu abaixo do nível de arco anterior e a barra atual abre acima do novo nível (`fibCrossUp`).
2. A LWMA rápida está acima da LWMA lenta (`bullishTrend`).
3. A distância absoluta entre o Momentum e 100 é de pelo menos `MomentumThreshold`.
4. A linha principal do MACD está acima de sua linha de sinal, ou acabou de cruzar para cima (`macdAboveSignal` ou `macdCrossUp`).
5. O tamanho atual da posição é menor ou igual a zero (sem exposição comprada existente).

A estratégia compra `Volume` mais o valor absoluto de qualquer exposição vendida aberta para garantir transições plano-para-comprado.

### Entradas curtas

Os trades vendidos espelham a lógica comprada:

1. `fibCrossDown` confirma um rompimento para baixo.
2. A LWMA rápida está abaixo da LWMA lenta.
3. A distância do Momentum excede `MomentumThreshold`.
4. O MACD está abaixo de sua linha de sinal ou cruza para baixo.
5. Nenhuma exposição comprada existente permanece.

### Saídas

As posições são fechadas quando um dos seguintes ocorre:

* As condições de tendência ou MACD se invertem contra o trade.
* O sinal de rompimento de Fibonacci oposto aparece.
* O nível adaptativo de stop-loss ou take-profit é tocado.

Todas as saídas são executadas com ordens de mercado para manter consistência com a versão MetaTrader.

## Gestão de risco

O EA original oferecia stops baseados em dinheiro, lógica de trailing e proteção de break-even. A estratégia StockSharp mantém as mesmas funcionalidades com parâmetros transparentes:

* `StopLossDistance` e `TakeProfitDistance` definem distâncias fixas em unidades de preço a partir do preço executado.
* `EnableBreakEven`, `BreakEvenTrigger` e `BreakEvenOffset` controlam o comportamento de mover para break-even.
* `EnableTrailing`, `TrailingTrigger` e `TrailingDistance` implementam um trailing stop baseado em velas.

## Parâmetros

| Nome | Descrição |
|------|-------------|
| `CandleType` | Período (e tipo de agregação) usado para todos os cálculos. |
| `FastMaPeriod`, `SlowMaPeriod` | Comprimentos LWMA que definem a tendência. |
| `MomentumPeriod`, `MomentumThreshold` | Configurações do filtro de Momentum. |
| `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` | Configuração do MACD. |
| `TrendAnchorLength`, `ArcAnchorLength`, `FibonacciRatio` | Controles de reconstrução do arco de Fibonacci. |
| `StopLossDistance`, `TakeProfitDistance` | Distâncias iniciais de stop e alvo (unidades de preço absolutas). |
| `EnableBreakEven`, `BreakEvenTrigger`, `BreakEvenOffset` | Lógica de break-even. |
| `EnableTrailing`, `TrailingTrigger`, `TrailingDistance` | Configuração do trailing stop. |

## Uso

1. Anexe a estratégia a um ativo e configure `Volume` de acordo com o tamanho de posição desejado.
2. Opcionalmente, ajuste o período, os comprimentos das médias móvias e as configurações de Fibonacci para o mercado alvo.
3. Inicie a estratégia. Todas as decisões dependem de velas concluídas; execução intrabarra não é necessária.
4. Revise os ajudantes de gráficos integrados para os painéis de LWMA rápida/lenta e MACD se o host suportar visualização.
