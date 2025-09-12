# US Index First 30m Candle Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Captures breakout of the first 30-minute range in the US session with one trade per day.

## Details

- **Entry Criteria**: After first 30m range is locked, price breaks above high or below low
- **Long/Short**: Both
- **Exit Criteria**: Stop at opposite range level, target at range size * risk reward
- **Stops**: Yes
- **Default Values**:
  - `RiskReward` = 1
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
