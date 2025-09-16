# Signal Count With Array Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy reproduces the logic of the MetaTrader 4 expert advisor `Signal-COunt-with array.mq4`.
It monitors Donchian channel extremes for a configurable set of price offsets and counts how often
the indicator output changes, becomes empty or returns to a signal value. The implementation keeps
the diagnostic focus of the original script: no trades are executed. Instead, the strategy prints
detailed statistics whenever a new high/low is registered or when per-candle logging is enabled.

## Concept

- Replace the original `iCustom` lookup of `super_signals_v2_alert` with a Donchian channel that
  provides the highest high and lowest low over `ChannelPeriod` candles.
- Evaluate a grid of offsets (`GapStart`, `GapStep`, `GapCount`) that emulate the multiple indicator
  configurations tested by the MQL script.
- For each offset track six counters that mirror the original arrays, including transitions into and
  out of the sentinel value (`2147483647` for empty upper readings and `-2147483646` for empty lower
  readings).
- Output a textual table with the accumulated counters so the user can inspect how often each buffer
  produces a fresh signal, returns to empty or leaves the zero default state.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `CandleType` | 5-minute time frame | Candle series used for the Donchian calculations. |
| `ChannelPeriod` | 24 | Number of candles used to determine the Donchian highs and lows. |
| `GapStart` | 0 | First offset (in multiples of the price step) applied to the virtual signal values. |
| `GapStep` | 1 | Step size (in price steps) between consecutive offsets. |
| `GapCount` | 8 | Number of offsets to evaluate (matches the original 0..7 loop). |
| `LogOnEachCandle` | false | When enabled, forces logging after every finished candle. |

## Counters

Every offset maintains two rows: index `0` represents the upper Donchian buffer (bullish signal) and
index `1` represents the lower buffer (bearish signal). The following statistics are collected:

- **Changed** – increments whenever the raw indicator value differs from the previous observation.
- **Empty** – counts how often the buffer returned the positive sentinel (`2147483647`).
- **NegEmpty** – counts occurrences of the negative sentinel (`-2147483646`), mainly for the lower buffer.
- **Zero** – tracks transitions from the default zero state to any non-zero value.
- **NewFromEmpty** – increments when a real price-based signal replaces the sentinel value.
- **BackToEmpty** – increments when the buffer falls back to its sentinel after holding a non-sentinel value.

These counters correspond one-to-one with the arrays maintained in the original Expert Advisor
(`GetInd_iCustom_changed`, `GetInd_iCustom_maxInt`, `GetInd_iCustom_minInt`, etc.).

## Logging

The strategy prints diagnostics through `AddInfoLog` in two situations:

1. Whenever the Donchian upper band rises or the lower band falls, indicating a fresh extreme.
2. Every finished candle when `LogOnEachCandle` is set to `true`.

Each log entry starts with the candle time and then lists the counters for each offset, making it easy
to compare behaviour across different virtual indicator configurations.

## Usage Notes

- Attach the strategy to any security; it relies only on historical candles and does not submit orders.
- Adjust `ChannelPeriod` to match the volatility of the instrument you are studying. A longer period
  mimics wider swing detection similar to the MT4 indicator.
- Increase `GapCount` if you need to observe more offsets. The arrays resize automatically on start.
- Combine the diagnostics with chart drawings (candles plus Donchian channel) to visually align the
  printed statistics with market structure.
