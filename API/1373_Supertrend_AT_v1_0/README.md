# Supertrend AT v1.0 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

A Supertrend-based strategy that enters a long position when the Supertrend flips from down to up and a short position when it flips from up to down. Position size is calculated from risk per trade, and exits use stop-loss and take-profit levels derived from the previous Supertrend.

## Details

- **Entry Criteria**: Supertrend direction change.
- **Long/Short**: Long and Short.
- **Exit Criteria**: Target or stop hit.
- **Stops**: Yes.
- **Default Values**:
  - `SupertrendLength` = 10
  - `SupertrendMultiplier` = 3m
  - `RiskPerTrade` = 2m
  - `RewardRatio` = 3m
  - `CommissionPercent` = 0.05m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend Following
  - Direction: Long & Short
  - Indicators: Supertrend
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
