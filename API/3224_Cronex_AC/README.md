# Cronex AC

The Cronex AC strategy recreates the classic Cronex Acceleration/Deceleration (AC) expert advisor using the StockSharp high-level API. It smooths the Accelerator Oscillator with two consecutive moving averages and reacts when the fast line crosses the slow line. Bullish crossovers open long positions and close shorts, while bearish crossovers open shorts and close longs.

## Trading logic

1. Build Accelerator Oscillator (AO-AC) values from the selected candle series.
2. Smooth the AC with the chosen moving-average type twice: the first smoothing produces the "fast" line and the second smoothing produces the "signal" line.
3. Evaluate the two lines on the bar defined by the `SignalBar` parameter. The strategy also looks one bar further back to confirm a crossover.
4. When the fast line crosses above the signal line, the strategy closes existing short positions (if enabled) and opens a new long position (if enabled).
5. When the fast line crosses below the signal line, the strategy closes existing long positions (if enabled) and opens a new short position (if enabled).
6. Position size equals the configured `Volume` plus the absolute value of the current position, allowing reversals in a single market order.

The logic mirrors the MQL5 expert by only acting on fully completed candles and by separating permissions for entries and exits in both directions.

## Parameters

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `SmoothingType` | `CronexMovingAverageType` | `Simple` | Moving-average algorithm applied to the Accelerator Oscillator. Options: Simple, Exponential, Smoothed, Weighted. |
| `FastPeriod` | `int` | `14` | Lookback of the first smoothing (fast line). |
| `SlowPeriod` | `int` | `25` | Lookback of the second smoothing (signal line). |
| `SignalBar` | `int` | `1` | Number of finished bars to look back when reading the signal. A value of 1 replicates the default Cronex behavior. |
| `CandleType` | `DataType` | `TimeFrame(8h)` | Candle series used for calculations. |
| `EnableLongEntry` | `bool` | `true` | Allow opening long positions after a bullish crossover. |
| `EnableShortEntry` | `bool` | `true` | Allow opening short positions after a bearish crossover. |
| `EnableLongExit` | `bool` | `true` | Allow closing long positions when the fast line drops below the slow line. |
| `EnableShortExit` | `bool` | `true` | Allow closing short positions when the fast line rises above the slow line. |
| `Volume` | `decimal` | strategy default | Order size used for entries. The strategy automatically adds the absolute value of the open position to reverse in a single trade. |

## Charting

When a chart area is available the strategy plots:

- source candles for the selected timeframe,
- Accelerator Oscillator values,
- fast and signal moving averages,
- the strategy's own trades for visual validation.

## Notes

- All calculations rely on completed candles (`CandleStates.Finished`) to avoid repainting.
- The smoothing buffers keep just enough historical values to evaluate the requested `SignalBar` shift, matching the original MQL expert.
- Money-management features from the MQL version (stop-loss, take-profit, deviation) are intentionally omitted so that position management can be handled externally through StockSharp's risk controls.
