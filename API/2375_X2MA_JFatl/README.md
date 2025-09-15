# X2MA JFATL Crossover Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy is a StockSharp adaptation of the MetaTrader expert `Exp_X2MA_JFatl`. It combines a fast Simple Moving Average (SMA) with a slow Jurik Moving Average (JMA) and an additional Jurik filter to confirm trend direction. Trades are opened when the fast average crosses the slow one and the price is on the same side of the filter. Positions are closed when price moves against the filter or an opposite crossover occurs.

## Details

- **Entry Criteria**:
  - **Long**: `SMA_fast` crosses above `JMA_slow` and `Close` > `JMA_filter`.
  - **Short**: `SMA_fast` crosses below `JMA_slow` and `Close` < `JMA_filter`.
- **Exit Criteria**:
  - Price moves to the opposite side of the filter.
  - Opposite crossover of the averages.
- **Long/Short**: Both sides.
- **Stops**: Not used by default.
- **Default Values**:
  - `Fast MA Length` = 5.
  - `Slow MA Length` = 12.
  - `Filter Length` = 20.
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Multiple (SMA, JMA)
  - Stops: No
  - Complexity: Moderate
  - Timeframe: Short-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
