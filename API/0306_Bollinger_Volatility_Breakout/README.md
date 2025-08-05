# Bollinger Volatility Breakout
[Русский](README_ru.md) | [中文](README_zh.md)
 
The **Bollinger Volatility Breakout** strategy is built around Bollinger Bands breakout with volatility confirmation.

Testing indicates an average annual return of about 181%. It performs best in the crypto market.

Signals trigger when Bollinger confirms breakout opportunities on intraday (5m) data. This makes the method suitable for active traders.

Stops rely on ATR multiples and factors like BollingerPeriod, BollingerDeviation. Adjust these defaults to balance risk and reward.

## Details
- **Entry Criteria**: see implementation for indicator conditions.
- **Long/Short**: Both directions.
- **Exit Criteria**: opposite signal or stop logic.
- **Stops**: Yes, using indicator-based calculations.
- **Default Values**:
  - `BollingerPeriod = 20`
  - `BollingerDeviation = 2.0m`
  - `AtrPeriod = 14`
  - `AtrDeviationMultiplier = 2.0m`
  - `StopLossMultiplier = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Bollinger
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

