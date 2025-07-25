# VWAP ADX Trend Strength
[Русский](README_ru.md) | [中文](README_zh.md)
 
The **VWAP ADX Trend Strength** strategy is built around VWAP with ADX Trend Strength.

Testing indicates an average annual return of about 46%. It performs best in the stocks market.

Signals trigger when its indicators confirms trend changes on intraday (5m) data. This makes the method suitable for active traders.

Stops rely on ATR multiples and factors like AdxPeriod, AdxThreshold. Adjust these defaults to balance risk and reward.

## Details
- **Entry Criteria**: see implementation for indicator conditions.
- **Long/Short**: Both directions.
- **Exit Criteria**: opposite signal or stop logic.
- **Stops**: Yes, using indicator-based calculations.
- **Default Values**:
  - `AdxPeriod = 14`
  - `AdxThreshold = 25m`
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

