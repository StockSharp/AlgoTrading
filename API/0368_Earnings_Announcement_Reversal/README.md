# Earnings Announcement Reversal
[Русский](README_ru.md) | [中文](README_zh.md)

The **Earnings Announcement Reversal** strategy shorts recent winners and buys recent losers on earnings announcement days.

## Details
- **Entry Criteria**: On earnings day, short stocks with positive recent returns and buy those with negative returns.
- **Long/Short**: Both.
- **Exit Criteria**: Position adjusted after signal; no explicit holding rule.
- **Stops**: No.
- **Default Values**:
  - `LookbackDays = 5`
  - `HoldingDays = 3`
  - `CandleType = TimeSpan.FromDays(1).TimeFrame()`
- **Filters**:
  - Category: Event-driven
  - Direction: Both
  - Indicators: Returns
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Daily
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
