# Donchian Volatility Contraction
[Русский](README_ru.md) | [中文](README_zh.md)
 
The **Donchian Volatility Contraction** strategy is built around Donchian Channel breakout after volatility contraction.

Signals trigger when Donchian confirms volatility contraction patterns on intraday (5m) data. This makes the method suitable for active traders.

Stops rely on ATR multiples and factors like DonchianPeriod, AtrPeriod. Adjust these defaults to balance risk and reward.

## Details
- **Entry Criteria**: see implementation for indicator conditions.
- **Long/Short**: Both directions.
- **Exit Criteria**: opposite signal or stop logic.
- **Stops**: Yes, using indicator-based calculations.
- **Default Values**:
  - `DonchianPeriod = 20`
  - `AtrPeriod = 14`
  - `VolatilityFactor = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Donchian
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
