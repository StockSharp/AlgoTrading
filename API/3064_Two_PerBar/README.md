# Two Per Bar Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

## Overview
The original MetaTrader expert "Two PerBar" opens a long and a short position at the very beginning of every new bar, closes the entire basket on the next bar, and optionally applies a martingale-like volume multiplier. The StockSharp port keeps the same rhythm by tracking both hedged legs explicitly and by reacting once per finished candle. All orders are created through the high-level API and respect the instrument metadata (price step, volume step, and min/max lot constraints).

## Trading cycle
1. **New candle detection** – the strategy subscribes to the configured candle series via `SubscribeCandles`. When the candle arrives with `State == CandleStates.Finished`, a fresh bar has started and the cycle runs.
2. **Evaluate take-profit hits** – each stored leg carries its own entry price and take-profit level. If the completed candle’s high or low touches that level, the leg is closed immediately with a market order and removed from the tracking list.
3. **Forced liquidation of leftovers** – any legs that survived the take-profit scan are liquidated at market before the next pair is opened. This mirrors the MetaTrader code that calls `PositionClose` on every bar open.
4. **Determine the next lot size** –
   - When a previous cycle still had open legs, the largest volume among them is multiplied by `VolumeMultiplier`.
   - When the basket finished flat (for example, both legs hit their take-profit), the cycle resets to `InitialVolume`.
   - `PrepareVolume` normalizes the candidate lot by rounding to two decimals, snapping it to the instrument `VolumeStep`, verifying against the exchange `MinVolume`, and finally resetting to `InitialVolume` if it exceeds either the user defined `MaxVolume` or the security’s `MaxVolume`.
5. **Update defaults** – the computed lot is stored inside `_lastCycleVolume` and written into `Strategy.Volume` so helper methods reuse the same amount.
6. **Spawn a fresh hedged pair** – `BuyMarket(volume)` opens the long leg and `SellMarket(volume)` opens the short leg. Each leg remembers the close price of the finished candle and the absolute take-profit level (`entry ± TakeProfitPoints * pointSize`). A zero or negative `TakeProfitPoints` disables the take-profit and only the forced liquidation step will exit the basket.

The result is a perpetual straddle: every candle begins with a long + short pair, both are inspected for profit targets during the bar, and everything is flat before the next cycle.

## Money management and protection
- **Martingale-like scaling** – `VolumeMultiplier` replicates the MetaTrader multiplier. When any leg survives until the forced liquidation step, the next cycle uses the heaviest leg’s size multiplied by this value. A completed profitable cycle (both legs closed via take-profit) resets the lot back to `InitialVolume`.
- **Volume capping** – `MaxVolume` is a hard cap that forces the lot back to `InitialVolume` once the multiplier would exceed it. The same reset happens if the instrument reports a tighter `Security.MaxVolume`.
- **Exchange compliance** – all volumes are snapped to the security `VolumeStep` and rejected when they fall below `MinVolume`. Setting `InitialVolume` to a tradable size guarantees that the reset path always remains valid.
- **Point calculation** – the take-profit offset uses `Security.PriceStep` (or `MinPriceStep` as a fallback). Instruments without a defined step effectively disable the take-profit because the computed offset is zero.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 1-minute time frame | Primary timeframe that triggers the once-per-bar workflow. |
| `InitialVolume` | `decimal` | `1` | Lot size used when starting a new cycle with no surviving legs. |
| `VolumeMultiplier` | `decimal` | `2` | Multiplier applied to the largest surviving leg from the previous cycle. |
| `MaxVolume` | `decimal` | `10` | Maximum permitted lot size before resetting to `InitialVolume`. |
| `TakeProfitPoints` | `int` | `50` | Distance in price points used to build the per-leg take-profit target. `0` disables take-profit and relies solely on the bar-close liquidation. |

## Implementation notes and differences
- Hedged legs are tracked manually inside `_legs` so that the strategy can reason about individual long/short exposures even though StockSharp reports only the net position.
- Instead of relying on individual ticks, the take-profit logic checks the completed candle’s high/low range. This keeps the implementation deterministic while staying faithful to the original "per bar" behavior.
- The MetaTrader slippage and magic number settings are not exposed; StockSharp handles order routing details, and the strategy runs on the portfolio associated with the parent strategy instance.
- Order placement uses the `Strategy` helper methods (`BuyMarket`, `SellMarket`) without adding indicators directly to `Strategy.Indicators`, complying with the repository guidelines.

## Usage tips
- Match `InitialVolume` to the instrument’s lot step before starting the strategy. The constructor does not attempt to automatically round your input.
- If the instrument has a very small price step, consider reducing `TakeProfitPoints`; otherwise the calculated take-profit may sit unrealistically far away.
- Because the strategy opens opposite-direction orders at the same time, run it on connectors/exchanges that allow hedged positions. In environments that net positions immediately, the `_legs` list still reflects the intended logic, but actual broker behavior may differ.
- Add the strategy to a chart to visualize candles and executed trades (`DrawCandles` + `DrawOwnTrades` are enabled in `OnStarted`).
