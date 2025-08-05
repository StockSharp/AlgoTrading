# Donchian Seasonal Filter
[Русский](README_ru.md) | [中文](README_zh.md)
 
The **Donchian Seasonal Filter** strategy is built around Donchian Channels with seasonal filter.

Testing indicates an average annual return of about 70%. It performs best in the stocks market.

Signals trigger when Donchian confirms filtered entries on intraday (15m) data. This makes the method suitable for active traders.

Stops rely on ATR multiples and factors like DonchianPeriod, SeasonalThreshold. Adjust these defaults to balance risk and reward.

## Details
- **Entry Criteria**: see implementation for indicator conditions.
- **Long/Short**: Both directions.
- **Exit Criteria**: opposite signal or stop logic.
- **Stops**: Yes, using indicator-based calculations.
- **Default Values**:
  - `DonchianPeriod = 20`
  - `SeasonalThreshold = 0.5m`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Donchian, Seasonal
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (15m)
  - Seasonality: Yes
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

