# Donchian Sentiment Spike
[Русский](README_ru.md) | [中文](README_zh.md)
 
The **Donchian Sentiment Spike** strategy is built around Donchian Sentiment Spike.

Signals trigger when Donchian confirms trend changes on intraday (15m) data. This makes the method suitable for active traders.

Stops rely on ATR multiples and factors like DonchianPeriod, SentimentPeriod. Adjust these defaults to balance risk and reward.

## Details
- **Entry Criteria**: see implementation for indicator conditions.
- **Long/Short**: Both directions.
- **Exit Criteria**: opposite signal or stop logic.
- **Stops**: Yes, using indicator-based calculations.
- **Default Values**:
  - `DonchianPeriod = 20`
  - `SentimentPeriod = 20`
  - `SentimentMultiplier = 2m`
  - `StopLoss = 2m`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Donchian, Spike
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (15m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
