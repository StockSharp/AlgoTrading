# Eliot Wave Strategy (Ported from MQL4 "Eliot Wave I")

## Overview

The **Eliot Wave Strategy** is a StockSharp API port of the original MetaTrader 4 expert
advisor "Eliot Wave I". The system combines a fast/slow Linear Weighted Moving Average
(LWMA) crossover with multi-timeframe momentum confirmation and a very slow MACD filter.
The goal is to identify impulsive moves in the direction of the prevailing trend while
keeping risk constrained through built-in protective rules.

## Core Indicators

- **Fast LWMA (default 6)** — tracks short-term direction using typical price
  `(High + Low + Close) / 3`.
- **Slow LWMA (default 85)** — measures broader trend on the same timeframe.
- **Momentum (default period 14)** — evaluated on a higher timeframe and converted to
  a deviation relative to the neutral level `100`. A reading above the configured threshold
  indicates a sufficiently strong impulse.
- **MACD (12, 26, 9)** — calculated on a very slow timeframe (monthly by default) and used as
  a long-term filter. The strategy only buys when the MACD main line is above the signal line
  and sells when it is below.

## Parameters

| Name | Description | Default |
| ---- | ----------- | ------- |
| `Base Candle` | Primary timeframe for LWMA processing. | 15-minute candles |
| `Momentum Candle` | Higher timeframe used for the momentum confirmation. | 1-hour candles |
| `MACD Candle` | Very slow timeframe for the MACD trend filter. | 30-day candles |
| `Fast LWMA` | Length of the fast linear weighted moving average. | 6 |
| `Slow LWMA` | Length of the slow linear weighted moving average. | 85 |
| `Momentum Period` | Lookback for the momentum indicator on the confirmation timeframe. | 14 |
| `Momentum Buy Threshold` | Minimum deviation above 100 required to validate a long setup. | 0.3 |
| `Momentum Sell Threshold` | Minimum deviation above 100 required to validate a short setup. | 0.3 |
| `Stop Loss (pts)` | Protective stop distance expressed in instrument points. | 20 |
| `Take Profit (pts)` | Target distance expressed in instrument points. | 50 |
| `Trade Volume` | Order size for each entry. | 1 lot |
| `Max Position` | Absolute net exposure allowed; prevents the strategy from exceeding the MQL EA's `Max_Trades` limit. | 10 lots |

All parameters are implemented as `StrategyParam<T>` so they can be optimised directly in
Designer or Runner.

## Trading Rules

1. **Trend and structure filter**
   - Fast LWMA must stay above the slow LWMA to consider long trades.
   - Fast LWMA must stay below the slow LWMA to consider shorts.
   - The last two completed candles must overlap (`Low[2] < High[1]` for buys,
     `Low[1] < High[2]` for sells), replicating the consolidation requirement from the EA.
2. **Momentum confirmation**
   - The higher timeframe momentum is transformed into `abs(momentum - 100)` values.
   - If any of the last three values exceeds the configured threshold the impulse is considered valid.
3. **Macro trend filter**
   - Buy trades require the MACD main line to be above the signal line on the slow timeframe.
   - Sell trades require the MACD main line to be below the signal line.
4. **Order execution**
   - When all conditions align the strategy sends a market order sized to reverse the
     current position and add the configured trade volume.
   - Position flips are supported so the behaviour matches the averaging logic of the original EA.

## Risk Management

- `StartProtection` automatically applies stop-loss and take-profit distances in instrument points.
- Additional exit logic closes long positions when the fast LWMA drops below the slow LWMA or when
  the MACD filter turns bearish (and vice versa for shorts). This mirrors the MQL exit blocks.
- The `Max Position` parameter prevents the strategy from accumulating exposure beyond the configured
  limit, respecting the EA's `Max_Trades` restriction.

## Differences from the Original EA

- Graphical trend-line checks and manual trade notifications were removed because they are specific
  to MetaTrader and have no StockSharp equivalent.
- Break-even and complex trailing-stop variants from the MQL script are replaced by the simpler
  `StartProtection` mechanism. Users can extend the strategy if those behaviours are required.
- Money-based equity protection is not implemented; risk is controlled through fixed stops and the
  position cap.

## Usage Notes

1. Attach the strategy to a liquid instrument and ensure the three candle streams are available.
2. Set the `Trade Volume`, stop/target distances, and thresholds according to the traded market's
   volatility.
3. Optimise thresholds separately for bullish and bearish impulses if the instrument exhibits
   asymmetric behaviour.
4. Consider enabling the built-in chart visuals (candles, LWMAs, trade markers) for easier debugging.

This port focuses on reproducing the signal logic of the original EA using the high-level StockSharp
API while keeping the implementation idiomatic and easy to maintain.
