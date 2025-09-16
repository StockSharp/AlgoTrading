# Pipso Range Reversal Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy is a StockSharp port of the Pipso MQL5 expert advisor. It acts as a mean-reversion system that sells into bullish breakouts and buys into bearish breakouts of a recent high/low range while limiting activity to a configurable trading session.

## Core Idea
- Build a Donchian-style channel from the highest high and lowest low of the last `LookbackPeriod` finished candles (defaults to 36).
- Monitor the upper boundary to fade upside breakouts and the lower boundary to fade downside breakouts.
- Open positions only when the current candle starts within the trading window defined by `StartHour` and `EndHour`.

## Trade Logic
### Entry Conditions
- **Short entry**: when the candle's high touches or exceeds the previous channel high, close any long position and, if within the session window, sell `OrderVolume` contracts at market. The model records the entry price as the channel high.
- **Long entry**: when the candle's low touches or breaks below the previous channel low, close any short position and, if trading is allowed, buy `OrderVolume` contracts at market with the channel low as entry reference.

### Exit Conditions
- Positions are closed immediately when price touches the opposite side of the channel (mirroring the original EA's behavior).
- A protective stop is placed at a fixed distance from the entry price. The stop distance equals `(channelHigh - channelLow) * (1 + StopRangePercent / 100)`; with the default `StopRangePercent = 300` the stop sits four channel widths away.
- Stops are evaluated on candle extremes: a long closes if the candle's low dips below the stop, and a short closes if the high exceeds the stop.

### Session Filter
- `StartHour` and `EndHour` are specified in exchange time. If `StartHour < EndHour` the strategy trades only between those hours on the same day. If `StartHour > EndHour` the window wraps across midnight, enabling night sessions (e.g., 21 → 9).
- When the window is disabled (`StartHour == EndHour`) the strategy stays flat.

## Parameters
- **OrderVolume** *(default 0.1)* – trading volume per order.
- **LookbackPeriod** *(default 36)* – number of candles used to compute the channel.
- **StartHour** *(default 21)* – hour (0–23) when the session opens.
- **EndHour** *(default 9)* – hour (0–23) when the session closes.
- **StopRangePercent** *(default 300)* – additional percentage of channel width added to the raw range before converting to a stop distance.
- **CandleType** *(default 1-hour candles)* – timeframe used for calculations.

## Indicators and Data
- Uses the `Highest` and `Lowest` indicators from StockSharp to track the channel boundaries.
- Works with any security that provides continuous candle data matching the selected `CandleType`.
- The original EA expects the chart timeframe to represent the decision horizon; you can adjust `CandleType` to reproduce those conditions.

## Notes
- The logic operates on finished candles to avoid intrabar noise; on live feeds the stop/entry prices approximate where the MQL5 EA would interact with ticks.
- No take-profit target is defined—profits are realized when price reverts to the opposite boundary or when the stop is hit.
- Consider calibrating session hours, range length, and stop multiplier to the trading instrument's volatility.

