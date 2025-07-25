# Keltner Reinforcement Learning Signal
[Русский](README_ru.md) | [中文](README_zh.md)
 
The **Keltner Reinforcement Learning Signal** strategy is built around Keltner Reinforcement Learning Signal.

Testing indicates an average annual return of about 118%. It performs best in the stocks market.

Signals trigger when Keltner confirms trend changes on intraday (15m) data. This makes the method suitable for active traders.

Stops rely on ATR multiples and factors like EmaPeriod, AtrPeriod. Adjust these defaults to balance risk and reward.

## Details
- **Entry Criteria**: see implementation for indicator conditions.
- **Long/Short**: Both directions.
- **Exit Criteria**: opposite signal or stop logic.
- **Stops**: Yes, using indicator-based calculations.
- **Default Values**:
  - `EmaPeriod = 20`
  - `AtrPeriod = 14`
  - `AtrMultiplier = 2m`
  - `StopLossAtr = 2m`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Keltner, Reinforcement
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (15m)
  - Seasonality: No
  - Neural Networks: Yes
  - Divergence: No
  - Risk Level: Medium

