# Hull MA Implied Volatility Breakout
[Русский](README_ru.md) | [中文](README_zh.md)
 
The **Hull MA Implied Volatility Breakout** strategy is built around Hull MA Implied Volatility Breakout.

Signals trigger when its indicators confirms breakout opportunities on intraday (15m) data. This makes the method suitable for active traders.

Stops rely on ATR multiples and factors like HmaPeriod, IVPeriod. Adjust these defaults to balance risk and reward.

## Details
- **Entry Criteria**: see implementation for indicator conditions.
- **Long/Short**: Both directions.
- **Exit Criteria**: opposite signal or stop logic.
- **Stops**: Yes, using indicator-based calculations.
- **Default Values**:
  - `HmaPeriod = 9`
  - `IVPeriod = 20`
  - `IVMultiplier = 2m`
  - `StopLossAtr = 2m`
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
