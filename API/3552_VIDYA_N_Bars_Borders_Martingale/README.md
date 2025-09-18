# VIDYA N Bars Borders Martingale

## Overview
The original MetaTrader strategy combines the "VIDYA N Bars Borders" channel indicator with a martingale position sizing module. The StockSharp port keeps the idea of buying when price drops below the adaptive lower band and selling when price rallies above the upper band. The channel centre is produced by an adaptive moving average (VIDYA analogue) and its width is controlled by an Average True Range envelope. A money management block increases the trade size after losing trades while observing maximum position and exposure limits.

## Trading logic
1. Subscribe to the selected timeframe candles.
2. Calculate a Kaufman Adaptive Moving Average as a VIDYA substitute and an ATR channel around it.
3. When the close of a finished candle crosses below the lower band, open or reverse into a long position (unless the `Reverse` flag is enabled, in which case a short is opened).
4. When the close crosses above the upper band, open or reverse into a short position (or a long if `Reverse` is true).
5. Enforce a minimum price distance between consecutive entries to avoid re-entering too close to the previous fill.
6. If the floating profit across the open position reaches the specified money target, flatten everything and wait for the next signal.
7. After each closed trade the next base volume is either reset to the initial size (after a profitable trade) or multiplied by the martingale ratio (after a losing trade). The resulting volume is aligned to the instrument step, and both per-trade and total volume caps are applied.

## Parameters
| Name | Description |
| --- | --- |
| `Candle Type` | Data type of candles to trade. |
| `CMO Period` | Efficiency ratio window for the adaptive moving average. |
| `EMA Period` | Smoothing period of the adaptive moving average. |
| `ATR Period` | Number of bars for the ATR channel half-width. |
| `Profit Target` | Money profit threshold that triggers a full exit. |
| `Increase Ratio` | Multiplier applied to the next trade volume after a losing trade. |
| `Max Position Volume` | Hard ceiling for a single order/position volume. |
| `Max Total Volume` | Upper bound on the total exposure opened by the strategy. |
| `Max Positions` | Maximum number of concurrent positions (the port keeps one net position). |
| `Minimum Step` | Minimum distance between two consecutive entries, measured in points. |
| `Base Volume` | Starting order size before martingale adjustments. |
| `Reverse Signals` | Inverts the long/short interpretation of the channel breakout. |

## Implementation notes
- StockSharp does not include a direct VIDYA implementation. The strategy uses `KaufmanAdaptiveMovingAverage` with configurable efficiency and smoothing windows to mimic the adaptive behaviour of VIDYA. This keeps responsiveness close to the original indicator while relying on built-in components.
- Only one net position is managed at a time. The MetaTrader version queued multiple pending entries; in StockSharp each signal either opens a fresh position or reverses the current one. Martingale scaling is applied to the next entry size instead of adding new layers immediately.
- Minimum step and volume alignment rely on the instrument metadata (`PriceStep`, `VolumeStep`, `MinVolume`, `MaxVolume`). Provide these values when configuring the strategy for accurate execution limits.
- Profit tracking is based on the strategy `PnL` and the latest candle close, which is sufficient for high-level backtests. For live trading connect the strategy to a portfolio that updates realized PnL values.

## Files
- `CS/VidyaNBarsBordersMartingaleStrategy.cs` — C# implementation of the strategy.
