# SilverTrend V3 Strategy (C#)

## Overview

The SilverTrend V3 strategy is a momentum-following system that originates from the MetaTrader 5 expert advisor "SilverTrend v3". The StockSharp port reproduces the original logic while adapting it to the high-level strategy API. The core idea is to detect bullish or bearish momentum using a SilverTrend channel calculation, confirm it with the J_TPO market profile oscillator, and manage the resulting positions with protective stops, trailing logic, and a Friday session filter.

## Signal Engine

1. **SilverTrend direction**
   - Uses a rolling 350-bar window with a 9-bar smoothing parameter to compute dynamic support (`smin`) and resistance (`smax`).
   - When the current close falls below `smin`, the system flags a bearish regime; a close above `smax` flips the regime to bullish.
   - The calculation iterates from the oldest bar to the most recent one to replicate the recursive nature of the original MQL code.

2. **J_TPO confirmation**
   - Implements the original 14-period J_TPO oscillator that measures how prices cluster within a short-term distribution.
   - Only allows long entries when the oscillator is positive and short entries when it is negative, filtering out weak momentum shifts.

3. **Signal change detection**
   - A trade is initiated only when the newly computed SilverTrend direction differs from the previous value, ensuring that the strategy reacts to genuine regime shifts instead of noise.

## Trade Management

- **Market entries** – The strategy trades the configured `Volume`. If an opposite position is open, it is closed and reversed in one market order.
- **Initial stop loss** – Optional. Defined in price steps relative to the fill price (converted with the instrument's `PriceStep`).
- **Take profit** – Optional. Also defined in price steps and evaluated against candle extremes to mimic the original order-modification behaviour.
- **Trailing stop** – Activates once price moves in favour by the configured trailing distance. For long positions the stop ratchets upward, for shorts it ratchets downward, matching the MetaTrader logic.
- **Exit on opposite signal** – When the previous regime points in the opposite direction, any existing position is liquidated on the next candle close.
- **Friday trading block** – New positions are skipped after the specified hour on Fridays to avoid weekend gaps, exactly as in the source EA.

## Parameters

| Name | Default | Description |
| --- | --- | --- |
| `TrailingStopPoints` | 50 | Trailing stop distance measured in price steps. Set to zero to disable trailing. |
| `TakeProfitPoints` | 50 | Take profit distance in price steps. Zero disables the target. |
| `InitialStopLossPoints` | 0 | Initial protective stop in price steps. Zero leaves the position without an initial stop. |
| `FridayCutoffHour` | 16 | Exchange hour after which no new entries are permitted on Friday. Use `0` to allow trading all day. |
| `CandleType` | 1-hour candles | Data series that feeds the indicators. Any supported timeframe can be used. |
| `Volume` | 1 lot | Trading size for each position (StockSharp `Volume` property). |

All distances are multiplied by `PriceStep` during runtime, which automatically adapts the strategy to the security's tick size (including 3/5 digit forex symbols).

## Data and Environment Requirements

- Requires at least 360 completed candles before live signals are produced so that both SilverTrend and J_TPO buffers are fully formed.
- Designed for single-instrument trading via `SubscribeCandles`. The `GetWorkingSecurities` override ensures the strategy subscribes only to the configured security and timeframe.
- Uses `StartProtection()` to enable the standard StockSharp position-protection service once at start-up.

## Usage Notes

- The algorithm expects trending instruments such as major forex pairs or liquid futures; adapt the timeframe to the market's volatility.
- Because the SilverTrend calculation is recursive, restarting the strategy with insufficient historical candles will delay signal formation until enough data is collected.
- The high-level API implementation uses candle extremes to simulate order management (stop loss, take profit, trailing). In live trading consider pairing the logic with actual stop/limit orders if your infrastructure requires it.
- The port stores internal state (`_previousSignal`, `_entryPrice`, trailing stops) exactly once per finished candle, matching the "one trade per bar" behaviour of the original EA.

## Conversion Details

- Faithfully reproduces the mathematical routines from `SilverTrend v3.mq5`, including the nested-array J_TPO algorithm.
- Applies C# best practices: parameters are exposed via `StrategyParam<T>`, all comments are in English, and indentation uses tabs as mandated by the repository guidelines.
- No Python version is provided in this release per the task requirements.
