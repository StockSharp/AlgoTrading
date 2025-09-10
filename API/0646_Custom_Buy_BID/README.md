# Custom Buy BID Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Custom Buy BID strategy uses the Supertrend indicator to identify bullish reversals. It opens a long position when price crosses above the Supertrend line and applies configurable profit and loss targets for risk management.

## Details

- **Entry Criteria**: Price crosses above Supertrend.
- **Long/Short**: Long only.
- **Exit Criteria**: Take Profit or Stop Loss.
- **Stops**: Yes.
- **Default Values**:
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3m
  - `TakeProfitPercent` = 5m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StartDate` = 2018-09-01
  - `EndDate` = 9999-01-01
- **Filters**:
  - Category: Trend Following
  - Direction: Long
  - Indicators: Supertrend
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
