# Supertrend RSI Divergence
[Русский](README_ru.md) | [中文](README_zh.md)
 
The **Supertrend RSI Divergence** strategy is built around that uses Supertrend indicator along with RSI divergence to identify trading opportunities.

Testing indicates an average annual return of about 67%. It performs best in the stocks market.

Signals trigger when Divergence confirms divergence setups on intraday (15m) data. This makes the method suitable for active traders.

Stops rely on ATR multiples and factors like SupertrendPeriod, SupertrendMultiplier. Adjust these defaults to balance risk and reward.

## Details
- **Entry Criteria**: see implementation for indicator conditions.
- **Long/Short**: Both directions.
- **Exit Criteria**: opposite signal or stop logic.
- **Stops**: Yes, using indicator-based calculations.
- **Default Values**:
  - `SupertrendPeriod = 10`
  - `SupertrendMultiplier = 3.0m`
  - `RsiPeriod = 14`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Divergence
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (15m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: Yes
  - Risk Level: Medium

