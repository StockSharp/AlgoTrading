# Seasonality Adjusted Momentum
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
The **Seasonality Adjusted Momentum** strategy is built around momentum indicator adjusted with seasonality strength.

Testing indicates an average annual return of about 172%. It performs best in the forex market.

The executed strategy uses **finished 5-minute candles**, not daily candles. Define `M = Momentum(14)`, `A = SMA(14) of M`, and `s = score[candle.OpenTime.Month]`. From flat, buy iff `s >= 0.5` and `M > A`; sell iff `s <= -0.5` and `M < A`. Exit long if `s < 0.5` or `M < A`; exit short if `s > -0.5` or `M > A`. After any signal order, wait 120 finished bars before the next signal. Protection is a **native 2% percent stop**, not ATR.

Published monthly scores Jan–Dec: `0.8, 0.2, 0.5, 0.7, 0.3, -0.2, -0.3, -0.4, -0.7, 0.4, 0.6, 0.9`. The packaged BTC/TON replay contains March only, allowing long entries (s=0.5) and forbidding new shorts at the default threshold. The claimed 172% annual return is **not independently verified**.

## Details
- **Entry Criteria**: see implementation for indicator conditions.
- **Long/Short**: Both directions.
- **Exit Criteria**: opposite signal or stop logic.
- **Stops**: Yes, using indicator-based calculations.
- **Default Values**:
  - `MomentumPeriod = 14`
  - `SeasonalityThreshold = 0.5m`
  - `StopLossPercent = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Seasonality, Adjusted
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: Yes
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

