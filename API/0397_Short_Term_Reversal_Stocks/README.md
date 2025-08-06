# Short-Term Reversal Stocks
[Русский](README_ru.md) | [中文](README_zh.md)

The **Short-Term Reversal Stocks** strategy applies mean reversion principles to equities. Each day the stocks with the largest losses over the prior week are bought while recent winners are shorted, betting on a short-lived reversal.

Positions are held for only a few days before re-evaluating.

## Details
- **Entry Criteria**: Daily ranking by one-week return.
- **Long/Short**: Both directions.
- **Exit Criteria**: Positions closed after several days or when rankings update.
- **Stops**: Volatility-based stop may be used.
- **Default Values**:
  - `CandleType = TimeSpan.FromDays(1).TimeFrame()`
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Price based
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
