# Safa Bot Alert Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Safa Bot Alert strategy uses a short SMA with an ADX filter to trade price crossovers. It enters long when price crosses above the SMA with strong trend strength and enters short on crosses below. Fixed take profit, stop loss and a trailing stop manage positions, and all trades close at a specified session time.

## Details

- **Entry Criteria**: Price crosses SMA and ADX > `AdxThreshold`.
- **Long/Short**: Both.
- **Exit Criteria**: Take profit, stop loss, trailing stop or session close.
- **Stops**: Fixed and trailing.
- **Default Values**:
  - `SmaLength` = 3
  - `TakeProfitPoints` = 80m
  - `StopLossPoints` = 35m
  - `TrailPoints` = 15m
  - `AdxLength` = 15
  - `AdxThreshold` = 15m
  - `SessionCloseHour` = 16
  - `SessionCloseMinute` = 0
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend Following
  - Direction: Both
  - Indicators: SMA, ADX
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
