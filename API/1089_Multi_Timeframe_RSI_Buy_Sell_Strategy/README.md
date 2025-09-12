# Multi Timeframe RSI Buy Sell Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses RSI values from three different timeframes. A long position is opened when all enabled RSI values are below the buy threshold. A short position is opened when all enabled RSI values are above the sell threshold. A cooldown period prevents consecutive signals.

## Details

- **Entry Criteria**: All enabled RSIs below/above thresholds.
- **Long/Short**: Both.
- **Exit Criteria**: Opposite signal.
- **Stops**: No.
- **Default Values**:
  - `Rsi1Length` = 14
  - `Rsi2Length` = 14
  - `Rsi3Length` = 14
  - `Rsi1CandleType` = TimeSpan.FromMinutes(5)
  - `Rsi2CandleType` = TimeSpan.FromMinutes(15)
  - `Rsi3CandleType` = TimeSpan.FromMinutes(30)
  - `BuyThreshold` = 30m
  - `SellThreshold` = 70m
  - `CooldownPeriod` = 5
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: RSI
  - Stops: No
  - Complexity: Basic
  - Timeframe: Multi-timeframe
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
