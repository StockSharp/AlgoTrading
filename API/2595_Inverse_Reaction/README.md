# Inverse Reaction Strategy

## Overview
The Inverse Reaction strategy is a mean-reversion system inspired by the original MetaTrader expert advisor "IREA". It reacts to unusually large single-bar moves and anticipates an inverse reaction on the next bar. The strategy computes a dynamic confidence level from recent candle ranges and only trades when price swings exceed that level but stay within user-defined bounds. Only one position can be open at any time.

## Trading Logic
1. **Inverse Reaction indicator** – For every finished candle the strategy measures the open/close change and feeds its absolute value into a simple moving average of length `MaPeriod`. The averaged change is multiplied by `Coefficient` to form a dynamic threshold similar to the original indicator's Dynamic Confidence Level (DCL).
2. **Signal validation** – The absolute open/close change of the latest candle must be greater than the dynamic threshold, greater than `MinCriteriaPoints * PriceStep`, and smaller than `MaxCriteriaPoints * PriceStep`. Signals are ignored if the previous candle already met the same condition, which mirrors the original expert advisor.
3. **Direction** – A negative change (bearish candle) suggests a rebound to the upside, so a long position is opened. A positive change implies a bearish reversal expectation and triggers a short position. New trades are sent only when there is no existing position.
4. **Risk management** – After entry, the strategy monitors subsequent candles. If the price touches the predefined stop-loss or take-profit levels (converted from points into absolute prices using the instrument's `PriceStep`), it immediately closes the open position using market orders. `StartProtection()` is also enabled to support built-in StockSharp protections.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `StopLossPoints` | Stop-loss distance in points (multiplied by `PriceStep`). |
| `TakeProfitPoints` | Take-profit distance in points. |
| `TradeVolume` | Volume used for every market order. |
| `SlippagePoints` | Informational setting that mirrors the MQL version; currently not applied to orders. |
| `MinCriteriaPoints` | Minimum open/close distance (in points) required for a valid signal. |
| `MaxCriteriaPoints` | Maximum allowed open/close distance (in points). |
| `Coefficient` | Multiplier used to build the dynamic confidence threshold. |
| `MaPeriod` | Length of the moving average used inside the indicator. Must be at least 3. |
| `CandleType` | Timeframe of the processed candles (default: 1 hour). |

## Usage Guidelines
- Ensure the selected instrument has a valid `PriceStep`. When unavailable, the strategy falls back to a step of 1.0, which may distort thresholds.
- Adjust `MinCriteriaPoints` and `MaxCriteriaPoints` to match the volatility of the chosen timeframe. Too narrow a window will filter out most signals, while too wide a window will allow extremely large moves that may not revert.
- The default `Coefficient` of 1.618 replicates the golden-ratio scaling from the original indicator. Higher values demand larger outlier candles before trading.
- Because positions are closed by market orders on the next candle close that breaches the stop or target levels, real execution may differ from the exact limit levels. Consider testing with intraday data for more precise control if necessary.
- Only one position is held at a time. The strategy will wait for the current trade to close before reacting to a new signal.

## Notes
- Backtest the configuration on historical data before using it live. The original EA was designed for FX markets; parameter tuning may be required for other assets.
- The `SlippagePoints` parameter is preserved for completeness but intentionally unused because StockSharp handles slippage differently from MetaTrader.
- Make sure that `MaPeriod` stays at or above 3; smaller values were prohibited in the original implementation and may lead to unstable thresholds.
