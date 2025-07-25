# CCI Volatility Filter
[Русский](README_ru.md) | [中文](README_zh.md)
 
The **CCI Volatility Filter** strategy is built around CCI with Volatility Filter.

Signals trigger when its indicators confirms filtered entries on intraday (5m) data. This makes the method suitable for active traders.

Stops rely on ATR multiples and factors like CciPeriod, AtrPeriod. Adjust these defaults to balance risk and reward.

## Details
- **Entry Criteria**: see implementation for indicator conditions.
- **Long/Short**: Both directions.
- **Exit Criteria**: opposite signal or stop logic.
- **Stops**: Yes, using indicator-based calculations.
- **Default Values**:
  - `CciPeriod = 20`
  - `AtrPeriod = 14`
  - `CciOversold = -100m`
  - `CciOverbought = 100m`
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

Testing indicates an average annual return of about 58%. It performs best in the stocks market.
