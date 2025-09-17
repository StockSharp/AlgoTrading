# PriceChannel Signal v2 Strategy

## Overview
PriceChannel Signal v2 is a trend-following breakout system built around a modified Donchian channel. The original MQL5 expert advisor watches for transitions in the channel trend, optional re-entry conditions when price pushes back through the bands, and protective exit levels derived from the same range. The StockSharp port keeps the original behaviour: it trades a single position at a time, reacts only on completed candles and can be restricted to an intraday window.

## Trading logic
1. Donchian channel high and low are calculated over the configured `ChannelPeriod`.
2. The raw range is shifted by two multipliers:
   * **Risk Factor** – compresses the entry bands towards the channel median.
   * **Exit Level** – builds a second pair of inner bands that trigger exits.
3. A trend state is maintained:
   * When the close breaks above the upper entry band the trend becomes bullish.
   * When the close breaks below the lower entry band the trend becomes bearish.
   * Otherwise the previous trend is kept.
4. Signals generated from that state:
   * **Long entry** – trend flips from bearish to bullish.
   * **Short entry** – trend flips from bullish to bearish.
   * **Long re-entry** – optional, price closes back above the upper band while the trend is already bullish.
   * **Short re-entry** – optional, price closes back below the lower band while the trend is already bearish.
   * **Long exit** – optional, price closes below the bullish exit band after being above it on the previous bar.
   * **Short exit** – optional, price closes above the bearish exit band after being below it on the previous bar.
5. Only one order per bar and per direction is allowed. The strategy refuses to open a new position if another one is already active.
6. If the intraday time filter is enabled, all of the signals above are ignored outside the configured window.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `ChannelPeriod` | Donchian lookback length used to calculate the price channel and exit bands. |
| `RiskFactor` | Shift of the entry bands (0–10). Lower values widen the bands, higher values tighten them. |
| `ExitLevel` | Shift of the exit bands. Must be larger than `RiskFactor` to stay inside the entry range. |
| `UseReEntry` | Enables re-entry trades when price pushes back through the active band. |
| `UseExitSignals` | Enables exit logic based on the inner protective bands. |
| `CandleType` | Timeframe used to build candles and run the indicators. |
| `UseTimeControl` | Toggles the intraday trading window. |
| `StartHour` / `StartMinute` | Inclusive beginning of the trading window when time control is active. |
| `EndHour` / `EndMinute` | Exclusive end of the trading window when time control is active. |

## Entry and exit rules
* **Enter long:** trend flips to bullish or re-entry condition fires, current position is flat, and the bar is inside the allowed time window.
* **Enter short:** trend flips to bearish or short re-entry condition fires, current position is flat, and the bar is inside the allowed time window.
* **Exit long:** `UseExitSignals` is enabled and the close falls below the exit band after being above it on the previous bar.
* **Exit short:** `UseExitSignals` is enabled and the close rises above the exit band after being below it on the previous bar.

## Additional notes
* The strategy works with market orders and does not pyramid positions.
* Indicator values are processed only on finished candles to avoid intrabar repainting.
* Volume defaults to 1 contract if not provided explicitly.
* Time control follows the original EA behaviour: the end time is exclusive, and wrapping across midnight is supported.
