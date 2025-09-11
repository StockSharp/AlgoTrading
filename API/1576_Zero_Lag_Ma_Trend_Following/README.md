# Zero-Lag MA Trend Following
[Русский](README_ru.md) | [中文](README_cn.md)

Trend following system that waits for a zero-lag MA to cross an EMA and then enters when price breaks out of an ATR-sized box. Positions include risk-reward based targets.

## Details

- **Entry Criteria**: Zero-lag MA cross and box breakout.
- **Long/Short**: Both directions.
- **Exit Criteria**: ATR-based stop or take profit.
- **Stops**: Yes.
- **Default Values**:
  - `Length` = 34
  - `AtrPeriod` = 14
  - `RiskReward` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: ZLEMA, EMA, ATR
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
