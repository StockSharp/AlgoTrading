# MOC Delta MOO Entry v2
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy records buy and sell volume during the afternoon session and uses the resulting MOC delta to trade the next day's open.

From 14:50 to 14:55 it accumulates high, low and separated buy/sell volume. At 14:55 it calculates the delta percentage of buy minus sell volume relative to total daily volume. At 8:30 the next day a long trade is taken if the delta is above the threshold and the open is above both 15 and 30 period SMAs. A short trade uses the opposite conditions. Positions include tick-based take profit and stop loss and are closed at 14:50.

## Details

- **Entry Criteria**: At 8:30, delta percent above threshold and price above SMA15 & SMA30 for long; delta below negative threshold and price below SMAs for short.
- **Long/Short**: Both.
- **Exit Criteria**: Take profit or stop loss; all positions closed at 14:50.
- **Stops**: Yes.
- **Default Values**:
  - `TpTicks` = 20
  - `SlTicks` = 10
  - `DeltaThreshold` = 2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: Volume, SMA
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: Yes
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
