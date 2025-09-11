# Fibonacci Swing Trading Bot
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy using Fibonacci retracement levels to trade swing moves.

This bot calculates 0.618 and 0.786 retracement levels from the last 50-bar range and opens positions when candles break above or below these levels. Risk management is handled through configurable stop loss and risk/reward parameters.

## Details

- **Entry Criteria**: Price action with Fibonacci levels.
- **Long/Short**: Both directions.
- **Exit Criteria**: Stop loss or take profit.
- **Stops**: Yes, percent based.
- **Default Values**:
  - `FiboLevel1` = 0.618
  - `FiboLevel2` = 0.786
  - `RiskRewardRatio` = 2
  - `StopLossPercent` = 1
  - `CandleType` = TimeSpan.FromHours(4)
- **Filters**:
  - Category: Swing
  - Direction: Both
  - Indicators: Fibonacci, Donchian
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: 4h
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

