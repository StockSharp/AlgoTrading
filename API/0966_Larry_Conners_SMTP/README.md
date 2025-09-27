# Larry Conners SMTP Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

A long-only strategy that buys after a 10-bar low when the current bar has the largest range in the last 10 bars and closes in the top 25% of its range. Entry is placed one tick above the high; stop-loss trails at successive lows.

## Details

- **Entry Criteria**:
  - **Long**: current low equals the lowest of the last 10 bars, today's range is the largest of the last 10, and the close is in the top 25% of the range; place a buy stop at `High + TickSize`.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - Trailing stop at the highest low since entry.
- **Stops**: Yes.
- **Default Values**:
  - `TickSize` = 0.01
  - `CandleType` = TimeSpan.FromDays(1).TimeFrame().
- **Filters**:
  - Category: Reversal
  - Direction: Long
  - Indicators: Highest, Lowest
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Daily
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
