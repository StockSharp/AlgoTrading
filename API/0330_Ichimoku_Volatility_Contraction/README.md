# Ichimoku Volatility Contraction
[Русский](README_ru.md) | [中文](README_zh.md)
 
The **Ichimoku Volatility Contraction** strategy is built around Ichimoku Volatility Contraction.

Signals trigger when its indicators confirms volatility contraction patterns on intraday (5m) data. This makes the method suitable for active traders.

Stops rely on ATR multiples and factors like TenkanPeriod, KijunPeriod. Adjust these defaults to balance risk and reward.

## Details
- **Entry Criteria**: see implementation for indicator conditions.
- **Long/Short**: Both directions.
- **Exit Criteria**: opposite signal or stop logic.
- **Stops**: Yes, using indicator-based calculations.
- **Default Values**:
  - `TenkanPeriod = 9`
  - `KijunPeriod = 26`
  - `SenkouSpanBPeriod = 52`
  - `AtrPeriod = 14`
  - `DeviationFactor = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: multiple indicators
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
