# Bollinger Breakout DC2008
[Русский](README_ru.md) | [中文](README_cn.md)

Reimplementation of Sergey Pavlov's (DC2008) MetaTrader Bollinger breakout expert advisor for the StockSharp high-level strategy API. The strategy watches completed candles, evaluates Bollinger Bands breakouts on the selected price source and opens or reverses positions only when the current trade is not losing.

## Overview
- Calculates a Bollinger Bands envelope on the configured timeframe and applied price (close, open, high, low, median, typical, weighted, or average).
- Generates **long** setups when the candle low closes below the lower band while the high remains under the middle band (strong downside stretch that should revert).
- Generates **short** setups when the candle high exceeds the upper band while the low stays above the middle band (strong upside stretch expected to revert).
- The original MQL expert traded on ticks; in this port signals are processed once per finished candle for stability and indicator coherence.
- Positions are only entered or reversed if the existing position shows a non-negative unrealized profit, replicating the original risk filter.

## Trading Logic
### Indicator pipeline
1. Subscribe to candles of the chosen `CandleType` (default: 1-hour time frame).
2. Feed the selected applied price into a Bollinger Bands indicator (`Length = BandsPeriod`, `Width = BandsDeviation`).
3. Ignore candles until the indicator produces valid upper, middle, and lower values.

### Entry conditions
- **Buy**: `Low < LowerBand` **and** `High < MiddleBand`. Indicates the entire candle traded below the middle line after piercing the lower band.
- **Sell**: `High > UpperBand` **and** `Low > MiddleBand`. Indicates the entire candle traded above the middle line after piercing the upper band.

### Position filter and management
- If there is **no position**, the strategy opens one market order with the configured `Volume` when a signal appears.
- If a position already exists:
  - When the signal is opposite to the current direction, compute unrealized profit as `Position * (Close - PositionPrice)` using the candle close.
  - If unrealized profit is **negative**, skip all actions for this candle (identical to the original early `return`).
  - If unrealized profit is **non-negative** and the signal is opposite, send a reversing market order sized `Volume + |Position|` to both flatten the current position and establish a new one in the signal direction.
  - Signals that match the current direction do not add to the position (same as the MQL version).
- There are no explicit stop-loss or take-profit orders; trade exits happen only via opposing signals that satisfy the profit filter.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `BandsPeriod` | 80 | Number of candles used to compute the Bollinger moving average and deviations. Must be positive and is exposed for optimization. |
| `BandsDeviation` | 3.0 | Standard deviation multiplier applied to the Bollinger Bands width. Positive, optimizable. |
| `AppliedPrice` | Close | Price source for the indicator: Close, Open, High, Low, Median, Typical, Weighted, or Average (OHLC/4). Mirrors `ENUM_APPLIED_PRICE` from MetaTrader. |
| `CandleType` | 1-hour time frame | Candle type (time frame) used for analysis and orders. Can be switched to any other data type supported by StockSharp. |
| `Volume` (inherited) | broker-dependent | Order size for new entries. Reversals automatically add the existing absolute position size. |

## Differences versus the original MQL expert
- The MetaTrader EA evaluated conditions on every tick; this C# port waits for finished candles to avoid acting on incomplete data.
- Indicator shift was fixed to zero in the source EA and remains implicit here; additional shifts are not exposed.
- MetaTrader reported floating profit directly; the port approximates it via candle close and `PositionPrice`, which is sufficient for sign comparison used by the filter.
- Trade management, string messages, and order comments from the MQL version are omitted, focusing purely on signal generation.

## Implementation notes
- Candles, indicators, and trading calls rely on StockSharp high-level APIs (`SubscribeCandles().Bind(...)`, `BuyMarket`, `SellMarket`).
- The indicator is drawn automatically if a chart area is available in the UI; trades are also plotted for debugging.
- The strategy resets and rebuilds the indicator on every start, so parameter changes take immediate effect on the next run.
