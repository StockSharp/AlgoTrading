# Escort Trend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Escort Trend Strategy combines a fast and slow Weighted Moving Average (WMA) with MACD and CCI confirmation. A long position is opened when the fast WMA is above the slow WMA, MACD main line crosses above the signal line, and CCI exceeds a positive threshold. A short position triggers on the opposite conditions. The strategy optionally uses fixed stop loss, take profit, and trailing stop.

## Details
- **Entry Criteria**:
  - **Long**: `FastWMA > SlowWMA` AND `MACD > Signal` AND `CCI > +Threshold`.
  - **Short**: `FastWMA < SlowWMA` AND `MACD < Signal` AND `CCI < -Threshold`.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Opposite entry signal.
  - Optional stop loss, take profit, or trailing stop.
- **Stops**: Yes, user-defined.
- **Default Values**:
  - `Fast WMA` = 8
  - `Slow WMA` = 18
  - `CCI Period` = 14
  - `CCI Threshold` = 100
  - `MACD Fast EMA` = 8
  - `MACD Slow EMA` = 18
  - `Take Profit` = 200
  - `Stop Loss` = 55
  - `Trailing Stop` = 35
  - `Trailing Step` = 3
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Multiple
  - Stops: Yes
  - Complexity: Moderate
  - Timeframe: Short-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
