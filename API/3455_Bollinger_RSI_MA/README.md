# Bollinger RSI MA Strategy

## Overview
The Bollinger RSI MA strategy ports the MetaTrader expert *BolRSIMAs* to the StockSharp high-level API. The system combines a
Bollinger Band breakout, an RSI filter and a higher timeframe exponential moving average (EMA) to identify pullback trades in
the direction of the dominant trend. Auto-lot sizing is preserved: when enabled the strategy converts the configured risk
fraction of portfolio equity into volume using the current price, the Bollinger stop distance and the instrument contract size.

## Trading logic
1. Subscribe to the primary candle series (default: 1 hour) and calculate Bollinger Bands and RSI on the same timeframe.
2. Subscribe to daily candles and feed their closing prices into a 200-period EMA to reproduce the higher timeframe filter used
   in the original EA.
3. Generate a **long** setup when the latest candle closes below the lower band, the RSI value is below the oversold threshold
   and the close remains above the daily EMA. A **short** setup is triggered by a close above the upper band, RSI above the
   overbought threshold and price below the daily EMA.
4. Open positions only when no exposure is active. Every new trade stores stop-loss and take-profit levels derived from the
   previous Bollinger values: longs use `lowerBand - StopLossOffset` and target the middle band; shorts use
   `upperBand + StopLossOffset` and target the middle band as well.
5. On each finished candle the strategy checks the candle extremums against the protective levels. If the low/high touches the
   stop or target, the position is closed immediately, emulating the protective orders placed by the MetaTrader version.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `CandleType` | 1-hour candles | Primary timeframe processed by Bollinger Bands and RSI. |
| `DailyCandleType` | 1-day candles | Higher timeframe that feeds the EMA trend filter. |
| `BollingerPeriod` | `20` | Number of candles used to build Bollinger Bands. |
| `BollingerDeviation` | `2` | Band width multiplier. |
| `RsiPeriod` | `13` | RSI smoothing length. |
| `RsiUpperLevel` | `70` | Overbought threshold required for short trades. |
| `RsiLowerLevel` | `30` | Oversold threshold required for long trades. |
| `MaPeriod` | `200` | Length of the higher timeframe EMA. |
| `StopLossOffset` | `0.0238` | Extra buffer added outside the band before placing the stop-loss. |
| `UseAutoLot` | `true` | Enables risk-based position sizing. |
| `RiskPerTrade` | `0.05` | Fraction of equity allocated to each trade when auto lot is active. |
| `FixedVolume` | `0.1` | Order size when auto lot sizing is disabled. |

## Money management
- When `UseAutoLot` is `true`, volume equals `(equity * RiskPerTrade) / (StopLossOffset * price * contractSize)` rounded to the
  exchange limits. This mirrors the MetaTrader autolot routine, which divides the risk amount by the stop distance in cash and
  the contract size.
- If equity information or price is unavailable, the strategy falls back to `FixedVolume` while still respecting the
  instrument volume constraints.

## Differences from the MetaTrader expert
- Stop-loss and take-profit orders are simulated through candle highs and lows instead of server-side orders, matching the
  outcome of the original EA without relying on synchronous order submission.
- The EMA filter uses StockSharp's candle subscriptions; there is no dependency on MetaTrader-specific daily data calls.
- Risk sizing honors StockSharp security limits (`MinVolume`, `MaxVolume`, `VolumeStep`) to avoid rejected orders on exchanges.

## Usage tips
- Adjust `StopLossOffset` when trading symbols with different price scales so that the distance reflects the original EA's
  2.38% buffer beyond the Bollinger Band.
- If the instrument uses a different daily timeframe (e.g., crypto exchanges), change `DailyCandleType` accordingly so the EMA
  reflects the intended trend filter.
- Combine the strategy with external trailing stops if you prefer dynamic exits once the middle band target is reached.
