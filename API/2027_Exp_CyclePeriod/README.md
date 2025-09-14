# Exp CyclePeriod Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses the CyclePeriod indicator to detect market cycle turns. It opens long positions when the indicator rises and short positions when it falls, closing opposite positions accordingly.

## Details

- **Entry Criteria:**
  - **Long**: CyclePeriod is rising and the current value is above the previous one.
  - **Short**: CyclePeriod is falling and the current value is below the previous one.
- **Long/Short**: Long and Short.
- **Exit Criteria:**
  - Close short when CyclePeriod turns upward.
  - Close long when CyclePeriod turns downward.
- **Stops**: Uses take profit and stop loss in price units.
- **Default Values:**
  - `CandleType` = TimeSpan.FromHours(6).TimeFrame().
  - `Alpha` = 0.07.
  - `SignalBar` = 1.
  - `TakeProfit` = 2000.
  - `StopLoss` = 1000.
  - `BuyPosOpen` = true.
  - `SellPosOpen` = true.
  - `BuyPosClose` = true.
  - `SellPosClose` = true.
- **Filters:**
  - Category: Trend following
  - Direction: Long & Short
  - Indicators: CyclePeriod
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: 6-hour
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
