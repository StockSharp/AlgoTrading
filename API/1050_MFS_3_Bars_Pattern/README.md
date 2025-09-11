# MFS 3 Bars Pattern Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy detects a three-bar bullish reversal sequence inside a downtrend. It looks for a large green "ignite" bar, a small red pullback, and a bullish confirmation bar closing above the pullback high. The trend filter requires long SMA > medium SMA > short SMA and the ignite close below the short SMA.

Once the pattern appears the strategy opens a long position, placing stop-loss at the ignite bar low and a take-profit at a configurable risk-reward multiple.

## Details

- **Entry Criteria**: Ignite bar, pullback bar, and confirmation bar in a downtrend.
- **Long/Short**: Long only.
- **Exit Criteria**: Stop-loss at ignite low or take-profit at risk-reward multiple.
- **Stops**: Yes, stop and target orders.
- **Default Values**:
  - `CandleType` = 15 minute
  - `SmaShortLength` = 20
  - `SmaMedLength` = 50
  - `SmaLongLength` = 200
  - `IgniteMultiplier` = 3
  - `MaxPullbackSize` = 0.33
  - `MinConfirmationSize` = 0.33
  - `RiskReward` = 2
- **Filters**:
  - Category: Pattern
  - Direction: Long
  - Indicators: Candlestick, Moving Average
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
