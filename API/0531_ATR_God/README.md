# ATR GOD
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that combines a Supertrend entry with ATR-based stop loss and take profit.

## Details

- **Entry Criteria**: Supertrend flip.
- **Long/Short**: Both directions.
- **Exit Criteria**: ATR stop or opposite signal.
- **Stops**: ATR-based.
- **Default Values**:
  - `Period` = 10
  - `Multiplier` = 3m
  - `RiskMultiplier` = 4.5m
  - `RewardRiskRatio` = 1.5m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: ATR, Supertrend
  - Stops: ATR
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

