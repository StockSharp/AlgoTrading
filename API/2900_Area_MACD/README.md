# Area MACD Strategy

## Overview
The Area MACD Strategy evaluates the balance between bullish and bearish momentum using the MACD main line. For every candle the strategy accumulates the sum of all positive MACD values and the absolute sum of all negative MACD values over a configurable history window. The dominant side defines the trading direction: a stronger positive area favours long positions, while a stronger negative area favours short exposure. A reverse switch allows trading against the detected trend when required.

The implementation uses the high-level StockSharp API with candle subscriptions and indicator bindings. Only completed candles are processed, and all trading logic is encapsulated inside the `ProcessCandle` handler.

## Indicators and Data
- **MACD (Moving Average Convergence Divergence)** with configurable fast, slow and signal periods.
- **Candles** of a user-defined timeframe (30 minutes by default).

## Trading Rules
1. **Long Entry** – When the cumulative positive MACD area is greater than the cumulative absolute negative area. If reverse mode is enabled the condition is inverted.
2. **Short Entry** – When the cumulative absolute negative MACD area dominates. Reverse mode swaps the behaviour.
3. **Position Management** – When a new entry signal appears the strategy closes any opposite position before opening the new one so that only a single directional position is held.

## Risk Management
- **Stop Loss** – Fixed distance in pips measured from the entry price. Converted automatically to price units using the security price step.
- **Take Profit** – Fixed profit target in pips using the same conversion rules.
- **Trailing Stop** – Optional trailing stop that activates once the position moves in profit by `TrailingStopPips + TrailingStepPips`. The stop then follows price with a gap defined by `TrailingStopPips` and only moves forward when the price advances by at least `TrailingStepPips` more. Both values must be greater than zero to enable the trailing logic.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `OrderVolume` | Order volume used for market entries. | 1 |
| `HistoryLength` | Number of candles stored for the MACD area comparison. | 60 |
| `MacdFastLength` | Fast EMA period for the MACD. | 12 |
| `MacdSlowLength` | Slow EMA period for the MACD. | 26 |
| `MacdSignalLength` | Signal EMA period for the MACD. | 9 |
| `ReverseSignals` | If enabled, swaps the long and short entry conditions. | false |
| `StopLossPips` | Stop loss distance expressed in pips. | 100 |
| `TakeProfitPips` | Take profit distance in pips. | 150 |
| `TrailingStopPips` | Trailing stop distance in pips. Set to zero to disable trailing. | 5 |
| `TrailingStepPips` | Additional progress required before the trailing stop is updated. Set to zero to disable trailing. | 5 |
| `CandleType` | Candle timeframe used by the subscription. | 30-minute time frame |

## Usage Notes
1. Attach the strategy to a portfolio and a security, then adjust the parameters for the target market.
2. Ensure that both `TrailingStopPips` and `TrailingStepPips` are greater than zero to enable trailing protection. Otherwise trailing is ignored and only stop loss / take profit levels are active.
3. Monitor the log messages for information about stop-loss, take-profit and trailing events. All logs are produced in English as required.

## Original Idea
The conversion is based on the MetaTrader 5 "Area MACD" expert advisor. The StockSharp version keeps the core concept of comparing MACD areas while integrating risk management and indicator handling through the framework's high-level API.
