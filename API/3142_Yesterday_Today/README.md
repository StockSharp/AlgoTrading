# Yesterday Today Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Yesterday Today Strategy reproduces the classic MetaTrader breakout where today's price is compared with yesterday's high and low. The strategy keeps track of the last completed daily candle, then watches intraday candles to react quickly when price escapes yesterday's range. Before reversing, it always closes any opposite exposure, delivering a clean one-position workflow.

## Overview

- Tracks the previous daily range and waits for the close of an intraday candle to break it.
- Opens long positions when the close exceeds yesterday's high; opens short positions when the close drops below yesterday's low.
- Applies fixed-distance stop-loss and take-profit levels expressed in pips. Pip size adapts to 3- or 5-digit forex quotes just like in the original MQL implementation.
- Risk levels are evaluated on every finished intraday candle using its high/low to detect stop-loss or take-profit hits.
- Uses the built-in protection framework to guard against unexpected margin issues.

## Workflow

1. Subscribe to daily candles and store the high/low of the last completed session.
2. Subscribe to intraday candles (15-minute by default) for signal evaluation.
3. On each finished intraday candle:
   - Exit immediately if the candle violates the active stop-loss or take-profit.
   - Enter long if the close is above yesterday's high and no long position is open.
   - Enter short if the close is below yesterday's low and no short position is open.
   - Any opposing position is closed first by increasing the market order volume.
4. Whenever a new daily candle completes, update the stored range for the next trading day.

## Parameters

- `TradeVolume` — lot size for new positions. When reversing, the strategy automatically adds the opposite exposure to flatten first.
- `StopLossPips` — distance from the entry price to the protective stop, expressed in pips. A value of `0` disables the stop.
- `TakeProfitPips` — distance from the entry price to the profit target, expressed in pips. A value of `0` disables the target.
- `SignalCandleType` — intraday candle type used for breakout detection (default is 15-minute candles).

## Details

- **Entry Criteria**: Intraday candle closes above yesterday's high (long) or below yesterday's low (short).
- **Long/Short**: Both directions supported.
- **Exit Criteria**: Stop-loss or take-profit levels touched by intraday candle extremes.
- **Stops**: Yes, fixed pip distances.
- **Default Values**:
  - `TradeVolume` = 1
  - `StopLossPips` = 50
  - `TakeProfitPips` = 50
  - `SignalCandleType` = `TimeSpan.FromMinutes(15).TimeFrame()`
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Price Action
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday entries with daily context
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

## Notes

- The strategy is designed for a single instrument. Configure `Security` and `Portfolio` before starting.
- Pip size is computed from `Security.PriceStep` and automatically scaled for 3 or 5 decimal forex symbols, mirroring the original EA logic.
- Protection is enabled in `OnStarted`, so global account safeguards remain active when the strategy trades.
