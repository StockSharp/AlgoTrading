# Seasonality Adjusted Momentum
[Русский](README_ru.md) | [中文](README_zh.md)
 
The **Seasonality Adjusted Momentum** strategy is built around momentum indicator adjusted with seasonality strength.

Testing indicates an average annual return of about 172%. It performs best in the forex market.

Signals trigger when Seasonality confirms momentum shifts on daily data. This makes the method suitable for active traders.

Stops rely on ATR multiples and factors like MomentumPeriod, SeasonalityThreshold. Adjust these defaults to balance risk and reward.

## Details
- **Entry Criteria**: see implementation for indicator conditions.
- **Long/Short**: Both directions.
- **Exit Criteria**: opposite signal or stop logic.
- **Stops**: Yes, using indicator-based calculations.
- **Default Values**:
  - `MomentumPeriod = 14`
  - `SeasonalityThreshold = 0.5m`
  - `StopLossPercent = 2.0m`
  - `CandleType = TimeSpan.FromDays(1).TimeFrame()`
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Seasonality, Adjusted
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Daily
  - Seasonality: Yes
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

