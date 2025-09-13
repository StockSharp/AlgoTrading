# Channel Trailing Stop
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy using Donchian channel breakout entries and trailing stop management.

The system opens trades when price closes outside the channel. A trailing stop tracks the opposite side of the channel plus offset. Optional "noose" trailing keeps stop loss at equal distance from current price to take profit. Pending orders can be cleared after fills.

## Details

- **Entry Criteria**: Close outside channel range.
- **Long/Short**: Both.
- **Exit Criteria**: Trailing stop or opposite signal.
- **Stops**: Trailing stop, optional noose.
- **Default Values**:
  - `TrailPeriod` = 5
  - `TrailStop` = 50
  - `UseNooseTrailing` = true
  - `UseChannelTrailing` = true
  - `DeletePendingOrders` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Donchian Channel
  - Stops: Trailing
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
