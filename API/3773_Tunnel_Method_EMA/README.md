# Tunnel Method EMA Strategy
[Русский](README_ru.md) | [中文](README_zh.md)

## Overview
The **Tunnel Method EMA Strategy** replicates the original MetaTrader "Tunnel Method" expert advisor on the StockSharp high-level API. It operates on hourly candles and compares three exponential moving averages (EMAs) built on closing prices:

- **Fast EMA (12 periods)** captures immediate momentum shifts.
- **Medium EMA (144 periods)** reflects the "tunnel" center used to validate short signals.
- **Slow EMA (169 periods)** provides the long-term directional filter for long trades.

The strategy keeps positions mutually exclusive (either long, short, or flat) and dynamically manages risk through explicit stop-loss, take-profit, and trailing-stop controls.

## Signal Logic
### Long Entries
1. Wait for a completed candle (no intrabar decisions).
2. Detect a bullish crossover where the fast EMA (12) moves from below to above the slow EMA (169).
3. Confirm that no position is currently open and submit a market buy order for the configured volume.

### Short Entries
1. Wait for a completed candle.
2. Detect a bearish crossover where the fast EMA (12) moves from above to below the medium EMA (144).
3. Confirm that no position is currently open and submit a market sell order.

### Position Management
- **Stop-Loss**: Closes the trade when price moves against the position by `StopLossPoints` (converted into absolute price using the security price step).
- **Take-Profit**: Locks in gains once price advances by `TakeProfitPoints` from the entry price.
- **Trailing Stop**: After the trade accumulates at least `TrailingTriggerPoints` of profit, the strategy trails the price using `TrailingStopPoints`. For long trades it follows the highest high since entry; for short trades it follows the lowest low since entry. A reversal to the trailing level closes the position.
- **State Reset**: After each exit (manual or protective) the internal trailing state resets to avoid interference with subsequent trades.

## Default Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `CandleType` | `TimeSpan.FromHours(1).TimeFrame()` | Hourly candles used for EMA calculations. |
| `FastLength` | 12 | Length of the fast EMA that reacts to recent price action. |
| `MediumLength` | 144 | Length of the tunnel center EMA for short validation. |
| `SlowLength` | 169 | Length of the tunnel boundary EMA for long validation. |
| `StopLossPoints` | 25 | Protective stop distance in instrument points. |
| `TakeProfitPoints` | 230 | Profit target distance in instrument points. |
| `TrailingStopPoints` | 35 | Distance maintained by the trailing stop once active. |
| `TrailingTriggerPoints` | 20 | Profit threshold required before trailing begins. |

## Filters & Characteristics
- **Category**: Trend-following crossover.
- **Instruments**: Works on any instrument that provides hourly candles and a reliable price step.
- **Direction**: Trades both long and short, never holding simultaneous positions.
- **Timeframe**: 1-hour candles by default (configurable through `CandleType`).
- **Risk Controls**: Hard stop-loss, take-profit, and trailing stop implemented inside the strategy logic.
- **Data Requirements**: Relies exclusively on candle close prices; no additional indicators or market depth are needed.

## Notes
- All indicator values are sourced from StockSharp's EMA implementations to ensure consistency with high-level API guidelines.
- The strategy ignores unfinished candles to avoid double-counting signals or acting on partial data.
- Trailing stop adjustments respect the security's `PriceStep` via `ShrinkPrice`, keeping exit levels aligned with valid tick increments.
- Default parameters mirror the original MQL settings but can be optimized through StockSharp's parameter optimization tools.
