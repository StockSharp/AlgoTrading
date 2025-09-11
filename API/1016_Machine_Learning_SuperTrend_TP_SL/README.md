# Machine Learning SuperTrend TP SL
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on SuperTrend indicator with trailing take profit and stop loss.

The stop and profit levels follow the SuperTrend line, aiming to capture sustained moves while locking in gains as momentum fades.

## Details

- **Entry Criteria**: Price crossing the SuperTrend line.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or hitting trailing take profit/stop loss.
- **Stops**: Yes, trailing by SuperTrend.
- **Default Values**:
  - `AtrPeriod` = 4
  - `AtrFactor` = 2.94m
  - `StopLossMultiplier` = 0.0025m
  - `TakeProfitMultiplier` = 0.022m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: ATR, SuperTrend
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
