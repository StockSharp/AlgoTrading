# Supertrend Hombrok Bot Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Supertrend strategy using volume, body size and RSI filters with ATR-based stop and take profit.

## Details
- **Entry Criteria**: Uptrend with volume and body filters and RSI below overbought for longs, downtrend with filters and RSI above oversold for shorts
- **Long/Short**: Both
- **Exit Criteria**: ATR-based stop loss or take profit
- **Stops**: Fixed stop and take profit from ATR
- **Default Values**:
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 3m
  - `RsiPeriod` = 14
  - `RsiOverbought` = 70m
  - `RsiOversold` = 30m
  - `VolumeMultiplier` = 1.2m
  - `BodyPctOfAtr` = 0.3m
  - `RiskRewardRatio` = 2m
  - `CapitalPerTrade` = 10m
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Supertrend, RSI, ATR, Volume
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
