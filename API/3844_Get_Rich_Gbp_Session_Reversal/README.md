# Get Rich GBP Session Reversal Strategy

## Overview
The **Get Rich or Die Trying GBP Strategy** is a high-frequency mean-reversion system that ported the MetaTrader 4 expert advisor "Get Rich or Die Trying GBP" to the StockSharp high-level API. The logic monitors a short rolling history of minute candles and opens trades near two predefined times of day when the most recent candles have mostly closed against the expected direction. This approach attempts to capture a quick retracement immediately after the London and New York sessions overlap.

## Trading Logic
1. The strategy subscribes to 1-minute candles by default (the candle type can be customized).
2. A rolling window of the last *Lookback* finished candles is maintained. Each candle is categorized as:
   - `+1` if it closed below its open (bearish candle).
   - `-1` if it closed above its open (bullish candle).
   - `0` if the candle is neutral.
3. The cumulative sum of these classifications is used to decide the trade direction:
   - A positive sum means bearish candles dominate and the strategy prepares for a **long** entry.
   - A negative sum means bullish candles dominate and the strategy prepares for a **short** entry.
4. Orders can be placed only during the first *EntryWindowMinutes* minutes after the hour when the current server time matches one of two target hours:
   - `FirstEntryHour + HourShift` (default: London midnight after the GMT+2 correction).
   - `SecondEntryHour + HourShift` (default: 21:00 server time for the New York close overlap).
5. If no position is open and all conditions are satisfied, the strategy sends a market order with either the fixed lot size or the dynamic size calculated from the money-management block.
6. While in a position, the strategy applies three independent exit rules:
   - A **partial take profit** closes the trade once the close price moves *PartialTakeProfitPoints* price steps in favor.
   - A **hard stop-loss** triggers when the price moves *StopLossPoints* price steps against the trade.
   - A **trailing stop** locks in profit after the market moves beyond *TrailingStopPoints* price steps, using the highest high (for longs) or the lowest low (for shorts) seen since the entry.
7. A final take-profit level equal to *TakeProfitPoints* price steps is also monitored as a safety net.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `TakeProfitPoints` | 100 | Maximum profit distance (in price steps) monitored after the trailing logic. |
| `PartialTakeProfitPoints` | 40 | Primary take-profit distance (in price steps) that replicates the original EA's early exit. |
| `StopLossPoints` | 100 | Stop-loss distance (in price steps). |
| `TrailingStopPoints` | 30 | Trailing stop distance (in price steps). |
| `FixedVolume` | 1 | Base order volume in lots when money management is disabled. |
| `UseMoneyManagement` | false | Enables dynamic position sizing based on account value and the configured risk. |
| `RiskPercent` | 10 | Percentage of portfolio value to risk per trade when money management is active. |
| `Lookback` | 18 | Number of finished candles used in the bullish/bearish count. |
| `FirstEntryHour` | 22 | First trading hour before the hour shift correction. |
| `SecondEntryHour` | 19 | Second trading hour before the hour shift correction. |
| `HourShift` | 2 | Time-zone correction applied to both trading hours. |
| `EntryWindowMinutes` | 5 | Width of the entry window (minutes from the start of the qualifying hour). |
| `CandleType` | 1-minute timeframe | Candle type to subscribe to; can be replaced with any other periodic candle type. |

## Money Management
When `UseMoneyManagement` is enabled, the strategy estimates the order volume by risking `RiskPercent` of the current portfolio value over the configured `StopLossPoints`. The calculation respects the instrument's lot step and minimum volume to remain exchange-compliant.

## Usage Notes
- The trading windows are evaluated using the exchange/server time of the incoming candles. Adjust `HourShift` so that `FirstEntryHour + HourShift` and `SecondEntryHour + HourShift` match the desired session boundaries.
- `Lookback` should remain greater than 1 to avoid noisy decisions. Increasing it smooths the sentiment measurement at the cost of slower reactions.
- The protective logic relies on finished candles. If intrabar precision is required, reduce the candle duration accordingly.
- The original MQL expert allowed multiple simultaneous positions; this port limits exposure to a single open position to match StockSharp best practices.

## Limitations
- The trailing stop is virtual and executes by sending a market exit on the next finished candle after the price crosses the trailing threshold.
- Money-management sizing assumes that `Security.StepPrice` correctly represents the monetary value of one price step. Validate this mapping for each instrument before live trading.

## Requirements
- StockSharp high-level API environment (AlgoTrading solution).
- Historical and real-time minute candles for the traded GBP instrument.

## References
- Original MetaTrader 4 expert advisor: `MQL/7690/Get_rich_or_die_trying_any_gbp.mq4`.
