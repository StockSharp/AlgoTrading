# Sensitive MACD Trailing Strategy

## Overview
This strategy is a direct StockSharp conversion of the "Sensitive" MACD expert advisor for MetaTrader 5. It combines MACD crossovers with configurable risk management tools (fixed stop loss, take profit, and pip-based trailing stops). The algorithm works exclusively on completed candles and uses the high-level API to subscribe to the desired timeframe.

## Indicators and Data
- **MACD (Moving Average Convergence Divergence)** – configured with independent fast, slow, and signal EMA lengths.
- **Candles** – user-selectable timeframe provided through the `CandleType` parameter.

## Entry Conditions
1. A new candle must close to avoid intrabar noise.
2. MACD values are processed from the indicator binding:
   - `macd` represents the MACD main line.
   - `signal` is the signal line (EMA of the MACD difference).
3. **Long entry** requirements:
   - MACD crosses above the signal line (`macd > signal` while the previous values satisfied `macd < signal`).
   - MACD remains in negative territory (`macd < 0`).
   - The absolute MACD magnitude is greater than `MacdOpenLevel * Point`, ensuring a meaningful displacement.
   - No open long position is active (net position is less than or equal to zero). Existing shorts are reversed by sending the necessary quantity.
4. **Short entry** requirements mirror the long setup:
   - MACD crosses below the signal line while remaining positive.
   - Absolute MACD magnitude exceeds the configured threshold.
   - No open short position exists (net position is greater than or equal to zero). Existing longs are flattened before opening the short.

## Exit Management
- **Take Profit**: Once the trade is opened, the strategy stores a target level defined by `TakeProfitPips`. If a long candle's high or a short candle's low reaches this level, the position is closed at market.
- **Stop Loss**: A protective stop is calculated from `StopLossPips`. For longs, a price drop to the stop level triggers a market exit. Shorts react to price increases reaching the stop.
- **Trailing Stop**: When `TrailingStopPips` is non-zero, the algorithm activates a trailing logic after the price advances by at least `TrailingStopPips + TrailingStepPips` pips from the entry. Subsequent movements tighten the stop level by always keeping the specified trailing distance from the latest close. The trailing step must be positive whenever the trailing stop is enabled; otherwise, the strategy stops with an error message.
- When no position is active, internal tracking variables are reset to prepare for the next trade.

## Position Sizing
Order quantities are controlled through the built-in `Volume` strategy parameter (default: 0.1). Reversals automatically add the absolute value of the current position to the desired volume to switch directions in a single market order.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `FastLength` | Fast EMA period used by the MACD main line. | 12 |
| `SlowLength` | Slow EMA period used by the MACD main line. | 26 |
| `SignalLength` | Signal EMA period for the MACD. | 9 |
| `MacdOpenLevel` | Minimum MACD magnitude (in price points) required to trigger trades. | 3 |
| `StopLossPips` | Distance of the protective stop in pips. | 35 |
| `TakeProfitPips` | Take-profit distance in pips. | 75 |
| `TrailingStopPips` | Trailing stop distance in pips (0 disables trailing). | 5 |
| `TrailingStepPips` | Additional distance price must move before the trailing stop updates. | 5 |
| `CandleType` | Source candle type (timeframe). | 1-minute candles |
| `Volume` | Order volume, expressed in lots/contracts depending on the instrument. | 0.1 |

## Additional Notes
- Pips and MACD point values are derived from the instrument's price step and decimal precision. The code adjusts for 3- and 5-digit forex symbols by scaling the pip size accordingly.
- All comments inside the source are written in English, and the implementation uses only high-level StockSharp APIs in line with the project guidelines.
- The strategy intentionally avoids partial fills management and assumes market orders are filled immediately when running in the simulator or real trading. Further safeguards can be added if needed.
