# Estratégia Starter Triple Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia porta o expert do MetaTrader **Starter.mq5** para a API de alto nível do StockSharp. Ela sincroniza três osciladores stochastic (rápido, normal, lento) com médias móveis correspondentes calculadas em diferentes períodos. Um trade é permitido apenas quando todos os filtros confirmam a mesma direção e o preço negocia no lado correto de cada média móvel deslocada.

## Lógica de negociação

1. A estratégia assina três fluxos de velas:
   - **Período rápido** (padrão `M5`).
   - **Período normal** (padrão `M30`).
   - **Período lento** (padrão `H2`).
2. Para cada fluxo, ela constrói uma média móvel (método configurável, comprimento e preço aplicado) e um oscilador stochastic com os mesmos parâmetros `%K`, `%D` e desaceleração.
3. O período lento impulsiona a execução. Quando uma vela lenta fecha, os valores mais recentes de todos os períodos são comparados:
   - Configuração longa: toda linha stochastic tem `%K > %D`, todos os valores `%K` estão abaixo de `50`, e o preço está abaixo de cada média móvel deslocada.
   - Configuração curta: toda linha stochastic tem `%K < %D`, todos os valores `%K` estão acima de `50`, e o preço está acima de cada média móvel deslocada.
4. Os sinais podem ser opcionalmente invertidos através de `ReverseSignals`. Quando uma entrada é tomada, a estratégia reverte a exposição existente (se `CloseOppositePositions = true`) ou ignora o sinal até que o posição oposta seja fechada.
5. Após um fill, os níveis de stop-loss e take-profit são simulados no espaço de preço. Um trailing stop replica a lógica MQL original exigindo `TrailingStopPips + TrailingStepPips` de lucro antes de mover o stop por `TrailingStopPips`.
6. O dimensionamento de posição baseado em risco espelha o interruptor `lot`/`risk` do MetaTrader. Quando o modo é `RiskPercent`, o volume do trade é derivado do valor da conta, a porcentagem de risco e a distância de stop-loss em pips.

## Parâmetros

| Nome | Padrão | Descrição |
|------|---------|-------------|
| `StopLossPips` | `45` | Distância protetora de stop em pips. Defina como `0` para desabilitar o stop fixo. |
| `TakeProfitPips` | `105` | Distância de take-profit em pips. Defina como `0` para desabilitar o alvo. |
| `TrailingStopPips` | `5` | Offset de trailing stop aplicado após o avanço mínimo. |
| `TrailingStepPips` | `5` | Avanço mínimo de lucro (em pips) necessário antes de o trailing stop se mover. |
| `MoneyMode` | `RiskPercent` | Seleciona entre dimensionamento de lote fixo e risco percentual por trade. |
| `MoneyValue` | `3` | Tamanho de lote ao usar `FixedLot`, ou percentual de risco ao usar `RiskPercent`. |
| `FastCandleType` | `M5` | Tipo de vela para o conjunto de indicadores rápido. |
| `NormalCandleType` | `M30` | Tipo de vela para o conjunto de indicadores intermediário. |
| `SlowCandleType` | `H2` | Tipo de vela que aciona a avaliação de sinais e ordens. |
| `MaPeriod` | `20` | Comprimento de todas as médias móveis. |
| `MaShift` | `1` | Deslocamento horizontal aplicado a cada média móvel (barras atrás). |
| `MaMethod` | `Simple` | Suavização da média móvel: `Simple`, `Exponential`, `Smoothed` ou `Weighted`. |
| `MaPriceType` | `Close` | Preço aplicado para alimentar as médias móveis. |
| `StochasticKPeriod` | `5` | Comprimento `%K` para todos os osciladores stochastic. |
| `StochasticDPeriod` | `3` | Comprimento de suavização `%D`. |
| `StochasticSlowing` | `3` | Fator de desaceleração final para `%K`. |
| `ReverseSignals` | `false` | Troca as condições longa e curta. |
| `CloseOppositePositions` | `false` | Se `true`, reverte a posição em uma única ordem quando um sinal aparece na direção oposta. |

## Gestão monetária

- `MoneyMode = FixedLot` envia cada ordem com o volume exato de `MoneyValue`.
- `MoneyMode = RiskPercent` reproduz o comportamento original: o valor arriscado é igual a `AccountValue * MoneyValue / 100`. O tamanho do trade é calculado como `valor arriscado / (StopLossPips * tamanho do pip)`. Se `StopLossPips` for zero ou o valor do portfólio não estiver disponível, a estratégia se recusa a negociar.

## Proteção e trailing

- Os níveis de stop-loss e take-profit são rastreados internamente e comparados com máximos/mínimos das velas, emulando as ordens protetoras do MetaTrader.
- O trailing stop é ativado apenas após o lucro não realizado exceder `TrailingStopPips + TrailingStepPips` pips, atendendo ao requisito original de que tanto um offset inicial quanto um passo mínimo devem ser satisfeitos antes de mover o stop.

## Alinhamento multiperíodo

Todos os indicadores são recalculados a cada vela fechada de seu respectivo período. O período lento aguarda que todas as três médias móveis e estocásticos se formem e usa os valores mais recentes da média móvel deslocada, imitando o parâmetro de shift `iMA` do MetaTrader. Isso garante que o port do StockSharp dispare trades na mesma barra que o expert MQL original.
