# Basic MA Template Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The **Basic MA Template Strategy** is a faithful StockSharp port of the MetaTrader 4 expert advisor from repository entry `MQL/27964`. The original robot traded a single symbol on a higher timeframe moving average and opened a position whenever the previous candle crossed the average. This C# version keeps the minimalist structure while exposing every control as a parameter so that the behaviour can be tuned or optimized directly inside StockSharp.

The template waits for a fully finished candle and compares its open and close prices with a shifted moving average. If the bar opens above the average and closes below it, the strategy opens a short position. When the reverse happens, it opens a long trade. The system only allows one market position at a time, mirroring the MQL "no active ticket" check. Protective stop-loss and take-profit distances are defined in pips. At start-up the strategy converts those pip distances into absolute price offsets using the instrument step and decimal precision, replicating the point-to-pip conversion logic that depended on quote digits in MetaTrader.

## Trading logic

- **Data source**: a single candle series determined by the `CandleType` parameter (default H4).
- **Indicator**: configurable moving average (`SMA`, `EMA`, `SMMA`, or `LWMA`). The `MovingAverageShift` parameter shifts the indicator forward exactly like the MetaTrader `iMA` function.
- **Entry rules**:
  - Long: the previous candle opened below and closed above the shifted moving average while no position is open.
  - Short: the previous candle opened above and closed below the shifted moving average while no position is open.
- **Exit rules**: handled automatically by the StockSharp `StartProtection` module using pip-based take-profit and stop-loss distances. When both targets are zero the strategy still enables the protection service so trailing or manual exits remain possible.
- **Position filter**: the strategy ignores new signals while a position is active, keeping behaviour identical to the original `PosSelect()` routine.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `CandleType` | Candle aggregation used for signals. | H4 (4-hour candles) |
| `MovingAveragePeriod` | Period length of the moving average. | 49 |
| `MovingAverageShift` | Forward shift applied to the moving average buffer. | 0 |
| `MovingAverageMethod` | Moving average calculation mode (`Simple`, `Exponential`, `Smoothed`, `LinearWeighted`). | `Simple` |
| `TakeProfitPips` | Take-profit distance in pips converted to absolute price offsets at runtime. | 38.5 |
| `StopLossPips` | Stop-loss distance in pips converted to absolute price offsets at runtime. | 48.5 |

### Risk handling

The protection subsystem receives the computed absolute distances and attaches them to every market order. Because the pip size is derived from the symbol step and decimal precision (5-digit and 3-digit quotes multiply the step by ten), the stop levels respect the minimum spacing enforced by brokers in the MetaTrader version.

### Conversion notes

- The original ECN-style two-step order placement is simplified to StockSharp market orders with automatic protection, which already handles attaching SL/TP after execution.
- The `CheckVolumeValue` and `CheckMoneyForTrade` routines are omitted. Position sizing should be configured through the standard StockSharp risk settings.
- Logging statements are replaced by chart drawing hooks so the moving average and executed trades can be visualized directly in the strategy chart area.

This conversion keeps the decision model identical while adopting idiomatic high-level StockSharp APIs (`SubscribeCandles`, `Bind`, and `StartProtection`). Use it as a lightweight scaffold for building more advanced moving-average systems.
