# Streak-Based Trading Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Tracks consecutive winning and losing candles. After the specified streak is reached, the strategy enters in the opposite direction and holds the position for a fixed number of candles. Doji candles are ignored based on body size.

## Details

- **Entry Criteria**: Opposite side after reaching win/loss streak.
- **Long/Short**: Configurable (`TradeDirection`).
- **Exit Criteria**: After `HoldDuration` candles.
- **Stops**: No.
- **Default Values**:
  - `TradeDirection` = Long
  - `StreakThreshold` = 8
  - `HoldDuration` = 7
  - `DojiThreshold` = 0.01
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Reversal
  - Direction: Configurable
  - Indicators: Price Action
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
