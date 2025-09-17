# Pendulum Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

A grid-based martingale system that swings between two price thresholds. The strategy opens a long position when price reaches the upper boundary of the grid and flips to a short position with increased volume when price moves to the lower boundary. It keeps alternating directions (up to a configurable number of layers) while expanding targets and reducing protective distances according to the original Pendulum expert advisor. After taking profit the engine resets the grid and schedules a fresh entry at the same level to keep the pendulum motion running.

## Details

- **Entry logic**
  - Aligns the grid to the candle close price using the configured `StepSize`.
  - **Upper trigger hit** → opens a long position with the base volume.
  - **Lower trigger hit** → opens a short position with the base volume.
  - When the active position moves to the opposite trigger the strategy reverses direction, multiplies the absolute volume by `Multiplier`, and updates take-profit / stop-loss distances like the MQL version.
  - Re-entries are scheduled after profitable exits so that the next candle can reopen at the same grid level once the previous orders are flat.
- **Exit logic**
  - Each layer defines a dedicated take-profit: one step for the first layer, `Multiplier` steps for every subsequent layer.
  - Protective stops mirror the MQL logic: first layer uses a wide stop (`StepSize * Multiplier`), subsequent layers use a one-step stop against the new direction.
  - When the maximum number of layers is reached the strategy waits for either take-profit or stop-loss before resetting.
- **Position management**
  - Uses netting: the StockSharp port closes and reverses the aggregate position instead of holding hedged longs and shorts. This preserves the exposure of the original expert while staying compatible with StockSharp portfolios.
  - Volume is rounded to the instrument volume step where available.
- **Data**
  - Works with any symbol and timeframe. The default subscription uses 1-minute candles and relies on candle close prices for the grid checks.
- **Built-in protection**
  - `StartProtection()` is enabled to guard unexpected positions left after disconnects or manual intervention.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `StepSize` | `0.001` | Distance between grid levels. The grid always snaps to multiples of this value. |
| `Multiplier` | `2` | Multiplies both the trade volume and extended targets whenever the direction flips to a new layer. Must be greater than 1. |
| `MaxLayers` | `3` | Maximum number of martingale layers before the strategy stops adding new reversals. |
| `BaseVolume` | `1` | Base trade size used for the first layer. Later layers scale by `Multiplier`. |
| `CandleType` | `1 Minute TimeFrame` | Candle type used for subscription. Can be changed to any other timeframe supported by the data source. |

## Notes

- The strategy recreates the behaviour of `Pendulum.mq5` without relying on hedged positions. Because StockSharp consolidates exposure, the net position is reversed to emulate the MQL grids.
- Take-profit completions trigger a deferred order so the next candle can reopen immediately at the same price level once the closing trade is processed.
- Keep the configured step size aligned with the instrument price step to avoid excessive rounding of the grid levels.
