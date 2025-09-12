# Consecutive Close High1 Mean Reversion Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Short-only strategy that counts consecutive closes above the prior high and sells once the count reaches a threshold. Position exits when price falls below the prior low. Optional 200 EMA filter confirms the downtrend.

## Details

- **Entry Criteria**: consecutive closes above previous high reach threshold
- **Long/Short**: Short
- **Exit Criteria**: close below previous low
- **Stops**: No
- **Default Values**:
  - `Threshold` = 3
  - `EmaPeriod` = 200
- **Filters**:
  - Category: Mean Reversion
  - Direction: Short
  - Indicators: EMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
