# NY ORB CP Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

NY opening range breakout strategy with retest confirmation. Trades breakouts of the 9:30-9:45 NY range when price retests and resumes the breakout direction.

## Details

- **Entry Criteria**:
  - Long: Price retests the NY high after breakout with trend and volume confirmation.
  - Short: Price retests the NY low after breakdown with trend and volume confirmation.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Profit target at 0.33 of range * `RiskReward`.
  - Stop loss at 0.33 of range.
- **Stops**: Yes, dynamic.
- **Default Values**:
  - `MinRangePoints` = 60
  - `RiskReward` = 3
  - `MaxTradesPerSession` = 3
  - `MaxDailyLoss` = -1000
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: EMA, VWAP, SMA
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Intraday
  - Seasonality: Yes
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
