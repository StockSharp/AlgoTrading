# 20/200 Expert Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy opens trades based on the difference between the opening prices of two past bars. It enters long when the open at shift2 minus the open at shift1 exceeds a threshold and enters short on the opposite condition. Positions are opened only at a specified hour and closed by take profit, stop loss or after a maximum holding time.

## Details

- **Entry Criteria:**
  - Long: open[Shift2] - open[Shift1] > DeltaLong points.
  - Short: open[Shift1] - open[Shift2] > DeltaShort points.
- **Long/Short:** Both.
- **Exit Criteria:** take profit, stop loss or max holding time.
- **Stops:** Fixed stop loss and take profit in points.
- **Default Values:**
  - Shift1 = 6
  - Shift2 = 2
  - DeltaLong = 6 points
  - DeltaShort = 21 points
  - TakeProfitLong = 390 points
  - StopLossLong = 1470 points
  - TakeProfitShort = 320 points
  - StopLossShort = 2670 points
  - TradeHour = 14
  - MaxOpenTime = 504 hours
  - Volume = 0.1
  - Candle timeframe = 1 hour
- **Filters:**
  - Category: Momentum
  - Direction: Long & Short
  - Indicators: None
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Hourly
  - Seasonality: Time-based
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
