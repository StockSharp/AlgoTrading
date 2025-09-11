# MOC Delta MOO Entry v2 Reverse Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy reverses the classic MOC Delta MOO Entry logic. It measures the buy-sell volume delta in the afternoon session (14:50–14:55) and stores the delta as a percentage of the day's volume. The next morning at 08:30 a position is opened in the opposite direction of the delta if it exceeds a threshold, filtered by two moving averages. Positions are closed with tick-based take profit and stop loss or at 14:50.

## Details

- **Entry Criteria**:
  - **Long**: At 08:30 when the saved delta percent is below `-DeltaThreshold` and the open price is above SMA15 and SMA30 with SMA15 above SMA30.
  - **Short**: At 08:30 when the saved delta percent is above `DeltaThreshold` and the open price is below SMA15 and SMA30 with SMA15 below SMA30.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Take profit and stop loss in ticks.
  - Close all open positions at 14:50.
- **Stops**:
  - `TpTicks` = 20 ticks take profit.
  - `SlTicks` = 10 ticks stop loss.
- **Default Values**:
  - `DeltaThreshold` = 2
  - `TpTicks` = 20
  - `SlTicks` = 10
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **Filters**:
  - Category: Volume
  - Direction: Both
  - Indicators: SMA
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
