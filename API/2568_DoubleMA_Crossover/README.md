# Double MA Crossover Breakout Strategy

## Overview

This strategy reproduces the "DoubleMA Crossover" MetaTrader expert adviser inside the StockSharp framework. The logic monitors a fast and a slow simple moving average, waits for a directional crossover, and then requires a breakout confirmation before entering the market. The algorithm manages only one position at a time and includes optional trailing stop behaviour that mimics the original three trailing modes.

## How It Works

1. **Signal detection** – Two simple moving averages (defaults: 2 and 5) are calculated on the selected candle series. A bullish crossover occurs when the fast average crosses above the slow one and vice versa for a bearish crossover.
2. **Breakout confirmation** – After a crossover the strategy stores a breakout level defined in price steps (`BreakoutPips`). A position is opened only when price reaches that level on a subsequent candle, replicating the stop order behaviour from the MQL version.
3. **Position management** – Only a single position is allowed. While a trade is active the strategy monitors stop-loss, take-profit, and the configured trailing stop type. The internal trackers emulate broker-side execution to keep behaviour deterministic in backtests.
4. **Session filter** – Trading can be restricted to a specific time window (`StartHour`..`StopHour`). The strategy still manages open trades outside the window but does not create new breakout levels when the filter blocks trading.
5. **Trailing stops** – Three trailing modes are supported: immediate trailing with the initial stop distance, trailing after a custom distance, and the three-level logic with breakeven shifts just like the original EA.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `FastMaPeriod`, `SlowMaPeriod` | Periods of the fast and slow simple moving averages. |
| `BreakoutPips` | Distance in price steps added to the signal candle close to define the breakout trigger. |
| `StopLossPips`, `TakeProfitPips` | Protective stop and optional take profit in price steps. Set take profit to zero to disable it. |
| `UseTrailingStop` | Enables trailing stop management. |
| `TrailingMode` | Trailing type: Type1 uses the original stop distance, Type2 waits for a custom distance (`TrailingStopPips`), Type3 uses the three MQL levels. |
| `TrailingStopPips` | Distance for Type2 trailing. |
| `Level1TriggerPips`, `Level1OffsetPips` | First trigger level and offset for Type3 trailing (moves stop to breakeven by default). |
| `Level2TriggerPips`, `Level2OffsetPips` | Second trigger level and offset for Type3 trailing. |
| `Level3TriggerPips`, `Level3OffsetPips` | Third trigger level and offset for Type3 trailing (converts to a classical trailing stop). |
| `UseTimeLimit`, `StartHour`, `StopHour` | Enables the trading session filter and defines the inclusive hour range. |
| `CandleType` | Candle series used for signal calculations. |
| `TradeVolume` | Order volume expressed in lots. |

## Trailing Stop Modes

- **Type1** – Moves the stop using the original stop-loss distance once price advances by that amount.
- **Type2** – Waits until price moves by `TrailingStopPips` before trailing, then locks profit at that distance.
- **Type3** – Uses three levels: the first two shift the stop by the defined offsets, and the third converts to a continuous trailing stop using the current close and `Level3OffsetPips`.

## Usage Tips

- Align `BreakoutPips` with the instrument tick size to maintain the same behaviour as the MetaTrader expert adviser.
- Review the session filter to match your trading hours; the default allows entries between 11:00 and 16:00 local time.
- Disable the time filter (`UseTimeLimit = false`) for 24/7 instruments.
- When testing trailing type 3, ensure the offset values are not larger than their corresponding trigger levels; otherwise the stop may remain behind the entry price.
