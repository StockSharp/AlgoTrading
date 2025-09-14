# Historical Volatility Ratio Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on the Historical Volatility Ratio (HVR). It compares short-term volatility over 6 bars to long-term volatility over 100 bars using log returns. When the ratio rises above the threshold, the system goes long expecting volatility expansion. When it falls below the threshold, the system goes short.

## Details

- **Entry Criteria**:
  - Long: `HVR > RatioThreshold`
  - Short: `HVR < RatioThreshold`
- **Long/Short**: Both
- **Exit Criteria**: Opposite signal
- **Stops**: No
- **Default Values**:
  - `ShortPeriod` = 6
  - `LongPeriod` = 100
  - `RatioThreshold` = 1.0
  - `CandleType` = `TimeSpan.FromMinutes(15).TimeFrame()`
- **Filters**:
  - Category: Volatility
  - Direction: Both
  - Indicators: Historical volatility (short and long)
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
