# Estratégia de Retração (Pull Back)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia Pull Back reproduz a lógica do expert advisor original do MetaTrader "PULL BACK" usando as APIs de alto nível do StockSharp. A abordagem busca retrações em direção a uma média móvel ponderada rápida em um período superior, confirma a força do momentum ao longo de várias barras e opera na direção da tendência mensal do MACD. Uma vez que uma posição é aberta, o algoritmo aplica regras de gestão de dinheiro que incluem stop-loss, take-profit, break-even e gerenciamento do trailing stop.

## Dados e indicadores

- **Período de trading:** tipo de candle selecionável pelo usuário (`CandleType`, padrão: candles de 15 minutos).
- **Período de confirmação:** assinatura de período superior (`HigherCandleType`, padrão: candles de 1 hora) usado para:
  - Médias móveis ponderadas rápida/lenta.
  - Indicador de momentum com distância absoluta do valor neutro (100).
  - Detecção de retração quando o candle anterior toca a WMA rápida.
- **Período MACD:** assinatura separada (`MacdCandleType`, padrão: candles de 30 dias) para ler a direção da linha de sinal do MACD.
- **Indicadores:**
  - Média Móvel Ponderada (WMA) nos períodos de trading e superior.
  - Momentum (período configurável) no período superior.
  - Moving Average Convergence Divergence (MACD) no longo período.

## Lógica de trading

### Configuração comprada

1. A WMA rápida do período superior está acima da WMA lenta.
2. O candle completado mais recente do período superior abriu acima da WMA rápida e a tocou com sua mínima (confirmação da retração).
3. Pelo menos uma das últimas três leituras de momentum absoluto supera `MomentumBuyThreshold`.
4. A linha principal do MACD está acima de sua linha de sinal no período MACD.
5. No período de trading, a WMA rápida está acima da WMA lenta.

Quando todas as regras estão satisfeitas, a estratégia envia uma ordem de compra de mercado. O preço de entrada é registrado para controlar os parâmetros de risco.

### Configuração vendida

1. A WMA rápida do período superior está abaixo da WMA lenta.
2. O candle recente abriu abaixo da WMA rápida e a tocou com sua máxima.
3. Um dos últimos três valores de momentum supera `MomentumSellThreshold`.
4. A linha principal do MACD está abaixo da linha de sinal.
5. A WMA rápida do período de trading está abaixo da WMA lenta.

Uma ordem de venda de mercado é enviada quando as condições se alinham.

## Gerenciamento de posição

- **Stop loss:** distância `StopLossTicks` da entrada (convertida para preço absoluto usando o passo de preço do instrumento).
- **Take profit:** distância `TakeProfitTicks` da entrada.
- **Break-even:** quando o preço avança `BreakEvenTriggerTicks`, o stop é movido para a entrada mais `BreakEvenOffsetTicks` na direção da operação se `UseBreakEven` estiver habilitado.
- **Trailing stop:** se `UseTrailingStop` for true, o stop acompanha o preço em `TrailingStopTicks` uma vez que a posição se mova em lucro.
- **Verificações de saída:** executadas em cada candle do período de trading completado. Se o stop ou o alvo for alcançado, a estratégia fecha a posição inteira com uma ordem de mercado.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `FastMaLength` | Comprimento da WMA rápida no período de trading (padrão: 6). |
| `SlowMaLength` | Comprimento da WMA lenta no período de trading (padrão: 85). |
| `BounceSlowLength` | Comprimento da WMA lenta no período de confirmação (padrão: 200). |
| `MomentumLength` | Lookback do Momentum no período superior (padrão: 14). |
| `MomentumBuyThreshold` | Mínimo |Momentum-100| para entradas compradas (padrão: 0.3). |
| `MomentumSellThreshold` | Mínimo |Momentum-100| para entradas vendidas (padrão: 0.3). |
| `StopLossTicks` | Distância do stop-loss em ticks (padrão: 200). |
| `TakeProfitTicks` | Distância do take-profit em ticks (padrão: 500). |
| `UseTrailingStop` | Habilitar lógica de trailing stop (padrão: true). |
| `TrailingStopTicks` | Distância do trailing stop em ticks (padrão: 400). |
| `UseBreakEven` | Habilitar ajuste de break-even (padrão: true). |
| `BreakEvenTriggerTicks` | Gatilho de lucro para break-even em ticks (padrão: 300). |
| `BreakEvenOffsetTicks` | Offset adicionado ao stop de break-even em ticks (padrão: 300). |
| `MacdFastLength` | Período EMA rápido do MACD (padrão: 12). |
| `MacdSlowLength` | Período EMA lento do MACD (padrão: 26). |
| `MacdSignalLength` | Período EMA de sinal do MACD (padrão: 9). |
| `CandleType` | Tipo de candle do período de trading. |
| `HigherCandleType` | Tipo de candle do período de confirmação. |
| `MacdCandleType` | Tipo de candle do período MACD. |

## Notas

- A estratégia espera que `Security.PriceStep` esteja populado para que os controles de risco baseados em ticks se traduzam corretamente para distâncias de preço.
- Apenas uma posição líquida é mantida por vez; sinais opostos são ignorados até que a posição atual seja fechada.
- A lógica processa apenas candles terminados para evitar atuar sobre dados parciais.
