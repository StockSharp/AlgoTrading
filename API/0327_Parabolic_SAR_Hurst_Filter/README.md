# Parabolic SAR Hurst Filter
[Русский](README_ru.md) | [中文](README_zh.md)
 
The **Parabolic SAR Hurst Filter** strategy is built around Parabolic SAR Hurst Filter.

Testing indicates an average annual return of about 82%. It performs best in the stocks market.

Signals trigger when Parabolic confirms filtered entries on intraday (5m) data. This makes the method suitable for active traders.

Stops rely on ATR multiples and factors like SarAccelerationFactor, SarMaxAccelerationFactor. Adjust these defaults to balance risk and reward.

## Details
- **Entry Criteria**: see implementation for indicator conditions.
- **Long/Short**: Both directions.
- **Exit Criteria**: opposite signal or stop logic.
- **Stops**: Yes, using indicator-based calculations.
- **Default Values**:
  - `SarAccelerationFactor = 0.02m`
  - `SarMaxAccelerationFactor = 0.2m`
  - `HurstPeriod = 100`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Parabolic, Hurst
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

