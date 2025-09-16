# ZigZag EvgeTrofi Strategy
[Русский](README_ru.md) | [中文](README_zh.md)

The ZigZag EvgeTrofi strategy ports the classic MetaTrader expert advisor into the StockSharp high level API. It watches the most recent swing detected by a ZigZag-style process and reacts quickly while the pivot is still fresh.

## Concept

* The original advisor analyses the first non-zero point of the ZigZag buffer and decides whether the last confirmed swing was a high or a low.
* A swing high generates a long entry by default. Activating **SignalReverse** inverts the logic.
* Positions are opened only while the new pivot is considered recent. The **Urgency** parameter limits the number of bars after a pivot when trades can be initiated.
* Existing positions in the opposite direction are flattened immediately before new orders are placed. The strategy can scale into the same direction on consecutive bars while the urgency window is open.

This port keeps the behaviour contrarian: new highs trigger long trades whereas fresh lows trigger shorts, mimicking the original setup.

## How it Works

1. Two rolling indicators (`Highest` and `Lowest`) approximate the MetaTrader ZigZag depth logic.
2. Whenever price prints a new extreme above/below those bands and the move exceeds **Deviation** (in price steps), a pivot is recorded.
3. The algorithm tracks how many bars passed since the pivot. Once the counter exceeds **Urgency** the signal expires.
4. On every closed candle during the active window the strategy enters using `VolumePerTrade`. Opposite exposure is closed first, so flip trades happen cleanly.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `Depth` | 17 | Window in bars to look back for swing highs/lows. Mirrors the ZigZag depth input. |
| `Deviation` | 7 | Minimum price displacement in points (multiplied by the symbol price step) required to accept a new pivot. |
| `Backstep` | 5 | Bars that must elapse before the indicator may switch to the opposite pivot direction. |
| `Urgency` | 2 | Maximum number of bars after the pivot when entries are allowed. |
| `SignalReverse` | `false` | Flips the mapping of highs/lows to long/short signals. |
| `CandleType` | 5 minute candles | Timeframe used for the analysis. Adjust to the chart you want to mirror. |
| `VolumePerTrade` | 0.10 | Order size submitted on every entry. Matches the original lot input. |

## Trading Notes

* The logic does **not** include stops or targets. Risk control must be added via overlays or portfolio settings if required.
* Because the system can add to a position every bar within the urgency window, position size may grow quickly on strong trends.
* Use higher depths on volatile symbols to avoid excessive pivots. Lower depths make the strategy more reactive but noisier.
* When **SignalReverse** is true the behaviour becomes breakout-following: swing highs trigger shorts and swing lows trigger longs.

## Files

* `CS/ZigZagEvgeTrofiStrategy.cs` – C# implementation of the strategy.
* Python version is intentionally not provided.
