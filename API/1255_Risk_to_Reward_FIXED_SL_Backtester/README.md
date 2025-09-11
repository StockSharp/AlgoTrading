# Risk to Reward Fixed SL Backtester Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Enters long when the close price matches a user-defined value. Stop loss is set by ATR or pivot low and take profit uses a risk-to-reward ratio or fixed percentage. Optionally moves the stop to breakeven after a target is reached.

## Details

- **Entry Criteria**: close price equals `DealStartValue`
- **Long/Short**: Long
- **Exit Criteria**: take profit or stop loss (optional breakeven)
- **Stops**: ATR or pivot low with breakeven
- **Default Values**:
  - `DealStartValue` = 100
  - `UseRiskToReward` = true
  - `RiskToRewardRatio` = 1.5
  - `StopLossType` = Atr
  - `AtrFactor` = 1.4
  - `PivotLookback` = 8
  - `FixedTp` = 0.015
  - `FixedSl` = 0.015
  - `UseBreakEven` = true
  - `BreakEvenRr` = 1.0
  - `BreakEvenPercent` = 0.001
- **Filters**:
  - Category: Trend
  - Direction: Long
  - Indicators: ATR, Lowest
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
