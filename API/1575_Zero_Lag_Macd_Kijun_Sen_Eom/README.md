# Zero Lag MACD + Kijun-sen + EOM Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy combining Zero Lag MACD with Kijun-sen baseline and Ease of Movement filter. Uses ATR-based stop and take profit.

## Details

- **Entry Criteria**: MACD cross with baseline and EOM filters.
- **Long/Short**: Both directions.
- **Exit Criteria**: ATR-based stop or take profit.
- **Stops**: Yes.
- **Default Values**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `MacdEmaLength` = 9
  - `KijunPeriod` = 26
  - `EomLength` = 14
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.5m
  - `RiskReward` = 1.2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: MACD, Donchian, EOM, ATR
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
