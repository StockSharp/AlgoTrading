# ADX Simple Trend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

## Overview
The **ADX Simple Trend Strategy** is a direct port of the classic MetaTrader expert advisor "ADX Simple". It follows the direction of the Average Directional Index (ADX) by comparing the positive and negative directional movement indicators (DI+ and DI-) and requiring the ADX main line to rise before opening any trade. The StockSharp version keeps the minimalist nature of the original system while adapting it to high-level API patterns and risk controls.

## Indicator Stack
- **Average Directional Index (ADX)** with configurable period (default 25).
  - Provides the **main ADX line** used to confirm trend strength.
  - Supplies **DI+** and **DI-** values that define bullish or bearish dominance.
- **Timeframe** is selectable through `CandleType` (defaults to 15-minute candles).

## Signal Generation
### Long Entry
1. Wait for a finished candle and a finalized ADX value.
2. Confirm that DI+ is above DI- on the same bar.
3. Require the ADX main line to be strictly greater than its previous value (trend is strengthening).
4. If no open position exists, send a market buy order using the strategy volume.

### Short Entry
1. Wait for a finished candle and finalized ADX reading.
2. Confirm that DI- is above DI+.
3. Require the ADX main line to be greater than its previous value.
4. If flat, send a market sell order with the strategy volume.

### Exit Logic
- **Close Long**: When DI- crosses above DI+ (trend momentum turns bearish).
- **Close Short**: When DI+ crosses above DI- (trend momentum turns bullish).
- The ADX slope check is not required for exits, mirroring the original EA which closed positions immediately after a DI crossover.

## Position Management
- The strategy is always either flat, long, or short; it never holds simultaneous positions in both directions.
- Market orders are sized using the built-in `Strategy.Volume` property (default 1). Adjust this property when configuring the strategy instance to match your instrument size.
- There are no automatic stop-loss or take-profit orders. Risk should be controlled externally or by modifying the strategy.

## Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `AdxPeriod` | `int` | 25 | Lookback length for ADX, DI+ and DI- computations. |
| `CandleType` | `DataType` | 15-minute time frame | Candle subscription used to drive indicator calculations. |

## Differences from the Original MQL Version
- Money management: the original EA resized lots based on account balance; the StockSharp strategy uses `Strategy.Volume` and leaves capital management to the hosting environment.
- Order tracking: instead of iterating through MetaTrader order pools, StockSharp relies on the built-in `Position` value.
- Data handling: the strategy ignores unfinished candles and only trades on finalized data.
- Logging and visualization hooks are available through `CreateChartArea`, `DrawCandles`, and `DrawIndicator` helpers for easier debugging.

## Usage Guidelines
1. Attach the strategy to an instrument with sufficient trend movement (e.g., FX majors or indices).
2. Set the desired candle type and ADX length through parameters before starting the strategy.
3. Optionally enable portfolio-level risk management (stop-outs, drawdown limits) through the hosting application.
4. Monitor DI crossovers and ADX slope in the chart visualizer to verify behaviour.

## Extending the Strategy
- Add volatility filters (ATR, standard deviation) to avoid low-volatility conditions.
- Introduce stop-loss/take-profit automation by calling `StartProtection` or custom order logic in `ProcessCandle`.
- Combine with higher timeframe filters by subscribing to additional candle streams.

This documentation aims to provide a comprehensive view of the ADX Simple Trend Strategy so that you can safely deploy and extend it within the StockSharp framework.
