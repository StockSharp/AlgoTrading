# Gann Laplace Smoothed Hybrid VSA
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy combines a Gann-style trend filter with Laplace-smoothed volume spread analysis (VSA). The VSA value is calculated as the price spread divided by the candle range and multiplied by volume, then smoothed with an EMA. Trades are taken when the smoothed VSA aligns with the price relative to the trend moving average.

## Details

- **Entry Criteria**:
  - **Long**: smoothed VSA > 0 and close > trend MA.
  - **Short**: smoothed VSA < 0 and close < trend MA.
- **Long/Short**: Both.
- **Exit Criteria**:
  - **Long**: smoothed VSA turns negative.
  - **Short**: smoothed VSA turns positive.
- **Stops**: Uses StartProtection.
- **Default Values**:
  - `Trend Period` = 20
  - `VSA Smoothing` = 14
  - `Candle Type` = 15m
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: MA, Volume
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Medium-term
