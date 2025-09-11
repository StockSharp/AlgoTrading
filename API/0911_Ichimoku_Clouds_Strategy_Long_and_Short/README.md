# Ichimoku Clouds Strategy Long and Short
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses the Tenkan-sen and Kijun-sen crossover of the Ichimoku indicator. Crosses are classified as strong, neutral or weak depending on the Tenkan value relative to the cloud. Depending on the selected trading mode, it opens long or short positions when the chosen signal strength occurs. Optional percentage-based take profit and stop loss can close positions or opposite signals as configured.

## Details

- **Entry Criteria**:
  - Tenkan-sen crosses above Kijun-sen and the signal strength matches selected long options.
  - Tenkan-sen crosses below Kijun-sen and the signal strength matches selected short options.
- **Long/Short**: Configurable, default long.
- **Exit Criteria**:
  - Opposite signals as defined by exit options.
  - Optional take profit or stop loss percentages.
- **Stops**: Percentage take-profit and stop-loss.
- **Default Values**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanPeriod` = 52
  - `TakeProfitPct` = 0
  - `StopLossPct` = 0
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Ichimoku
  - Stops: Optional
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
