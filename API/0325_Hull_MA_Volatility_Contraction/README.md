# Hull MA Volatility Contraction
[Русский](README_ru.md) | [中文](README_zh.md)
 
The **Hull MA Volatility Contraction** strategy is built around Hull Moving Average with volatility contraction filter.

Testing indicates an average annual return of about 76%. It performs best in the forex market.

Signals trigger when its indicators confirms volatility contraction patterns on intraday (15m) data. This makes the method suitable for active traders.

Stops rely on ATR multiples and factors like HmaPeriod, AtrPeriod. Adjust these defaults to balance risk and reward.

## Details
- **Entry Criteria**: see implementation for indicator conditions.
- **Long/Short**: Both directions.
- **Exit Criteria**: opposite signal or stop logic.
- **Stops**: Yes, using indicator-based calculations.
- **Default Values**:
  - `HmaPeriod = 9`
  - `AtrPeriod = 14`
  - `VolatilityContractionFactor = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: multiple indicators
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (15m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

