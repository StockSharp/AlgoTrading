# 2pbIdeal XOSMA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy is a C# translation of the MQL5 expert adviser **Exp_2pbIdealXOSMA**. It analyzes the slope of the MACD histogram to determine market momentum. When the histogram rises for two consecutive bars, the system enters a long position and closes any open short. When the histogram falls for two consecutive bars, the strategy enters a short position and closes any open long.

By default the algorithm operates on 4-hour candles but the timeframe is configurable. All trades are placed at market price and the position is reversed when the opposite signal appears. No stop-loss or take-profit is applied inside the sample; risk control can be added externally if desired.

## Details

- **Entry Criteria**:
  - **Long**: Histogram at bar `t-1` is below `t-2` and the current histogram exceeds `t-1`.
  - **Short**: Histogram at bar `t-1` is above `t-2` and the current histogram is below `t-1`.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal closes the current position.
- **Stops**: None.
- **Default Values**:
  - `FastPeriod` = 10
  - `SlowPeriod` = 26
  - `SignalPeriod` = 9
  - `SignalBar` = 1
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Single (MACD)
  - Stops: No
  - Complexity: Simple
  - Timeframe: 4-hour (configurable)
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
