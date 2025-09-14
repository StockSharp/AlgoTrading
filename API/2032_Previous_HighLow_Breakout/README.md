# Previous High Low Breakout Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Breakout strategy that monitors the previous candle's high and low on a chosen timeframe. A long position is opened when the new candle closes above the prior high, while a short position is opened when the close falls below the prior low. A trailing stop and fixed take profit manage risk and lock in gains.

The method aims to capture strong directional moves after consolidation. Trailing stops keep risk tight as price moves in the favorable direction.

## Details

- **Entry Criteria**:
  - Long: `Close > PreviousHigh`
  - Short: `Close < PreviousLow`
- **Long/Short**: Both
- **Exit Criteria**:
  - Stop loss or take profit
- **Stops**: Absolute with trailing using `StopLoss` and `TakeProfit`
- **Default Values**:
  - `StopLoss` = 50m
  - `TakeProfit` = 1000m
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: None
  - Stops: Yes (trailing)
  - Complexity: Beginner
  - Timeframe: Long-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

