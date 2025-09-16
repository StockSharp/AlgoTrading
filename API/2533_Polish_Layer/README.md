# Polish Layer Strategy

## Overview
The **Polish Layer Strategy** is a conversion of the MetaTrader expert advisor from `MQL/17484` into the StockSharp high-level API. It targets short-term trend continuation on forex pairs using 5-minute or 15-minute candles. Trend direction is defined by the relationship between fast and slow exponential moving averages and the recent momentum of the Relative Strength Index (RSI). Entry confirmation requires synchronized signals from Stochastic Oscillator, DeMarker, and Williams %R oscillators.

## Indicators
- **Exponential Moving Average (EMA)** – fast (`ShortEmaPeriod`) and slow (`LongEmaPeriod`) trend filters.
- **Relative Strength Index (RSI)** –  momentum slope filter derived from prior candle values.
- **Stochastic Oscillator** – detects oversold/overbought reversals via %K threshold crosses.
- **DeMarker** – confirms accumulation/distribution phases.
- **Williams %R** – validates momentum reversals at extreme levels.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `ShortEmaPeriod` | 9 | Length of the fast EMA trend filter. |
| `LongEmaPeriod` | 45 | Length of the slow EMA trend filter. |
| `RsiPeriod` | 14 | RSI lookback used for momentum slope comparison. |
| `StochasticKPeriod` | 5 | Lookback of the %K line. |
| `StochasticDPeriod` | 3 | Smoothing period for %D. |
| `StochasticSlowing` | 3 | Final slowing factor applied to %K. |
| `WilliamsRPeriod` | 14 | Williams %R lookback window. |
| `DeMarkerPeriod` | 14 | DeMarker lookback window. |
| `TakeProfitPoints` | 17 | Distance to the profit target in price points (uses `Security.PriceStep`). |
| `StopLossPoints` | 77 | Distance to the protective stop in price points. |
| `CandleType` | 5-minute | Candle data type processed by the strategy. |
| `Volume` | 1 | Trade size used for market entries. |

## Trading Logic
1. **Trend filter** – the previous candle must show the fast EMA above the slow EMA and RSI rising (previous RSI > RSI from two bars ago) for long scenarios. The inverse configuration defines short scenarios.
2. **Oscillator confirmation** – entries are only considered when the strategy is flat and all of the following conditions are met:
   - **Stochastic %K** crosses above 19 for longs or below 81 for shorts.
   - **DeMarker** crosses above 0.35 for longs or below 0.63 for shorts.
   - **Williams %R** crosses above -81 for longs or below -19 for shorts.
3. **Order execution** – the strategy submits market orders using `BuyMarket(Volume)` or `SellMarket(Volume)` and relies on `StartProtection` to attach stop-loss and take-profit offsets automatically.

## Risk Management
- Protective orders are created via `StartProtection`, transforming `TakeProfitPoints` and `StopLossPoints` into absolute price distances based on the instrument `PriceStep`.
- The algorithm remains out of the market until existing positions are closed by the protective orders, mirroring the behaviour of the original expert advisor.

## Usage Notes
- Works best on liquid forex pairs with 5-minute or 15-minute candles.
- Ensure the security metadata contains a valid `PriceStep`; otherwise, adjust `TakeProfitPoints` and `StopLossPoints` to match the instrument tick size.
- Consider forward-testing before live deployment because the confirmation sequence is sensitive to indicator smoothing and broker pricing increments.
