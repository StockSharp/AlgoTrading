# XKRI Histogram Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on the Kairi Relative Index (KRI) smoothed by an exponential moving average. The system looks for local minima and maxima of the smoothed oscillator and enters long or short positions when a reversal pattern appears.

## Details

- **Entry Criteria**:
  - Long: `Kri[1] < Kri[2] && Kri[0] > Kri[1]`
  - Short: `Kri[1] > Kri[2] && Kri[0] < Kri[1]`
- **Long/Short**: Both
- **Stops**: Point take profit and stop loss
- **Default Values**:
  - `KriPeriod` = 20
  - `SmoothPeriod` = 7
  - `TakeProfit` = 2000
  - `StopLoss` = 1000
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Filters**:
  - Category: Reversal
  - Direction: Both
  - Indicators: Kairi, EMA
  - Stops: Yes
  - Complexity: Beginner
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
