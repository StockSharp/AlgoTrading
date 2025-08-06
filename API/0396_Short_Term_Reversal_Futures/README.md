# Short-Term Reversal Futures
[Русский](README_ru.md) | [中文](README_zh.md)

The **Short-Term Reversal Futures** strategy seeks mean reversion across futures contracts. Each day the strategy identifies contracts with the worst return over the previous week and buys them while selling contracts that rallied the most, expecting a snap back.

Trades are held for a few days before closing on the next signal.

## Details
- **Entry Criteria**: Daily ranking by trailing one-week return.
- **Long/Short**: Both directions.
- **Exit Criteria**: Positions closed after a short holding period or when ranking updates.
- **Stops**: Volatility-based stop may be applied.
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
