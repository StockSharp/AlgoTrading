# Supertrade RVI Long-Only Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Uses Relative Volatility Index crossing above 20 to open long trades. Stop loss and take profit are set from risk percent and reward ratio.

## Details

- **Entry Criteria**: RVI crosses above threshold
- **Long/Short**: Long
- **Exit Criteria**: Stop loss or take profit
- **Stops**: Yes
- **Default Values**:
  - `RviLength` = 10
  - `EmaLength` = 14
  - `RviThreshold` = 20
  - `RiskPercent` = 1
  - `RewardRatio` = 3
- **Filters**:
  - Category: Momentum
  - Direction: Long
  - Indicators: StdDev, EMA
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

