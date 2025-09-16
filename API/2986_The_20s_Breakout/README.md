# The 20s Breakout Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

## Overview
This strategy is a C# conversion of the MetaTrader expert advisor **Exp_The_20s_v020**. It reproduces the original "The 20s" indicator logic that searches for breakout patterns after a volatility squeeze. The algorithm analyses completed candles from a configurable timeframe and reacts when price pushes through 20% bands around the previous bar's range. The implementation keeps the high-level feel of the StockSharp API and exposes all trading permissions so you can enable or disable long or short actions independently.

## Signal Logic
The indicator monitors the most recent candles and calculates reference levels from the previous bar:

1. Measure the range of the previous candle: `range = high[1] - low[1]`.
2. Build two thresholds around that bar:
   - `top = high[1] - range * Ratio`
   - `bottom = low[1] + range * Ratio`
3. Compare the current candle against the thresholds and the `LevelPoints` distance (converted to price using the instrument's `PriceStep`).

The original code exposes two calculation modes:

- **Mode1 (default)** – looks for a false breakout inside the 20% band on the previous candle followed by a strong rejection on the current candle. Depending on `IsDirect`, the strategy either buys the dip (`Direct = true`) or sells it (`Direct = false`).
- **Mode2** – requires a series of three expanding candles prior to the signal. If the compression bursts to the downside and price opens below the lower band, one direction is triggered; if it opens above the upper band, the opposite direction is triggered. `IsDirect` again flips the direction to match the original EA behaviour.

The `SignalBar` parameter postpones execution by several bars (0 = current candle, 1 = previous candle, etc.). This reproduces the expert advisor's ability to act on older signals once they are fully formed.

## Trade Management
- **Entries**: `AllowLongEntry` and `AllowShortEntry` control whether new positions are opened. The `OrderVolume` parameter defines the trade size for any fresh position.
- **Position reversals**: When a bullish signal appears the strategy first covers any short exposure (`AllowShortExit`) and then optionally opens a long position. The bearish signal mirrors this logic for long positions.
- **Stops & targets**: `StopLossPoints` and `TakeProfitPoints` are measured in instrument points. They are converted to prices using `PriceStep` and evaluated on every completed candle. If either level is touched, the position is closed immediately.
- **Direct mode**: Setting `IsDirect` to `true` mimics the original indicator outputs. Switching it to `false` inverts the arrow directions, which is useful when you want to mirror the behaviour on markets with different characteristics.

## Parameters
- `OrderVolume` – default `1`. Lot size used for new positions.
- `StopLossPoints` – default `1000`. Protective stop in points (`0` disables it).
- `TakeProfitPoints` – default `2000`. Profit target in points (`0` disables it).
- `AllowLongEntry` / `AllowShortEntry` – enable long/short entries.
- `AllowLongExit` / `AllowShortExit` – allow the strategy to close existing positions when opposite signals occur.
- `SignalBar` – default `1`. Number of bars to wait before acting on a signal.
- `LevelPoints` – default `100`. Distance that confirms breakouts beyond the previous bar extremes.
- `Ratio` – default `0.2`. Width of the 20% bands around the previous candle.
- `IsDirect` – default `false`. Keeps the original buy/sell mapping when `true`, flips it when `false`.
- `Mode` – default `Mode1`. Selects between the two calculation algorithms.
- `CandleType` – default `H1` time frame. Defines the subscription used for the calculations.

## Notes
- The strategy works on completed candles only; partial candles are ignored to avoid premature trades.
- All log entries and inline comments are in English to keep the code consistent with StockSharp samples.
- The stop and target management is handled inside the strategy and does not rely on additional orders, which makes the behaviour portable across simulators and live brokers.
- You can attach the strategy to any instrument. Just ensure the `PriceStep` property is available so point-based distances are converted correctly.
- Consider combining `Mode2` with a larger `SignalBar` on higher time frames to emulate the EA's "wait for confirmation" behaviour.
