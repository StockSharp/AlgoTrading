# 5min RSI Qualified Strategy

## Overview

The **5min RSI Qualified Strategy** is a direct conversion of the MetaTrader expert advisor "5min_rsi_qual_01a". The original robot looked for exhaustion in five-minute candles using a 28-period Relative Strength Index (RSI). Once the oscillator stayed in an extreme zone for a predefined number of bars, the EA opened a contrarian position and attached a trailing stop that followed the close of the previous candle. The StockSharp port keeps the exact confirmation logic, price offsets, and single-position restriction while relying on the high-level candle subscription API.

By default the strategy operates on five-minute candles, but the `CandleType` parameter accepts any other time frame supported by the instrument. All indicator thresholds and stop distances remain expressed in MetaTrader "points" so users can reapply their tested configurations without further adjustments.

## Trading Logic

1. **RSI calculation** – A 28-period RSI is updated on each finished candle. Only completed candles are processed to match the MQL4 `Close[1]` reference.
2. **Qualification counters** – Two counters keep track of how many consecutive candles the RSI has stayed above the overbought threshold (`UpperThreshold`) or below the oversold threshold (`LowerThreshold`). This mirrors the MQL loop that inspected the last 12 bars.
3. **Entry conditions** – When no position is open and the overbought counter reaches `QualificationLength`, the strategy sells at market. Conversely, when the oversold counter reaches the requirement, it buys at market. This reproduces the EA's behaviour of holding at most one trade per symbol.
4. **Trailing stop** – While a position is active, the stop level is recalculated on every finished candle using the previous close minus/plus `StopLossPoints` converted to absolute price. The stop only moves in the direction of the trade, exactly like the `OrderModify` calls in the original code.
5. **Initial stop** – After each fill the strategy sets the initial stop using `InitialStopPoints`. If the initial value is tighter than the trailing distance, the trailing logic will not loosen it, preserving the MetaTrader behaviour where the initial stop could be closer than the trailing distance.

## Risk Management

- Stop distances are defined in MetaTrader points to match the EA. They are converted to absolute price increments using the instrument's `PriceStep` (or `MinStep` when the primary step is unavailable).
- The strategy never pyramids trades. A new position is only opened once the previous one has been fully closed.
- `StartProtection()` is invoked on startup so that StockSharp's protective infrastructure stays in sync with the manually managed stop levels.

## Parameters

| Parameter | Description | Default |
| --- | --- | --- |
| `RsiPeriod` | RSI lookback length. | `28` |
| `QualificationLength` | Number of consecutive candles the RSI must stay in the extreme zone before a signal is confirmed. | `12` |
| `UpperThreshold` | RSI level that qualifies a bearish setup. | `55` |
| `LowerThreshold` | RSI level that qualifies a bullish setup. | `45` |
| `StopLossPoints` | Trailing stop distance in MetaTrader points. Converted to absolute price on every candle. Set to `0` to disable trailing. | `21` |
| `InitialStopPoints` | Initial protective stop distance in MetaTrader points applied immediately after entry. Set to `0` to skip the initial stop. | `11` |
| `CandleType` | Candle type used for signal evaluation (5-minute by default). | `5-minute time frame` |

## Usage Guidelines

- Ensure the instrument's price step matches the point size used during MetaTrader optimisation. For five-digit FX symbols, one point equals 0.00010 (one pip), so the default distances reproduce the EA's 11/21-point offsets.
- Because the method is contrarian, signals are more reliable in ranging markets. Consider widening the thresholds or increasing `QualificationLength` for trending assets.
- The strategy uses the base class `Volume` property for order size. Configure it in the UI or via code before starting the strategy.
- Optimisation can be performed on the RSI thresholds, qualification length, and stop distances thanks to the `SetCanOptimize()` flags.

## Conversion Notes

- Candle handling, RSI calculation, and the one-position restriction mirror the MetaTrader implementation. No additional filters were introduced.
- The trailing stop updates the stop level with the previous candle's close just like the MQL4 `Close[1]` logic, ensuring both versions exit at the same price when a reversal occurs.
- Error checks from the MQL4 script (bar count, free margin) are intentionally omitted because StockSharp handles data readiness and portfolio availability internally.
