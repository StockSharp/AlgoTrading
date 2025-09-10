# Automatic Trendlines Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Builds dynamic support and resistance trendlines by connecting recent pivot highs and lows. A long signal occurs when price closes above the resistance line, while a short signal fires when price drops below the support line.

## Details

- **Entry Criteria**:
  - **Long**: Close crosses above resistance trendline.
  - **Short**: Close crosses below support trendline.
- **Exit Criteria**:
  - Opposite signal or position reversal.
- **Indicators**:
  - Pivot-based trendlines.
- **Stops**: None.
- **Default Values**:
  - `LeftBars` = 100
  - `RightBars` = 15
- **Filters**:
  - Trend-following
  - Single timeframe
  - Indicators: pivot trendlines
  - Stops: none
  - Complexity: Low
