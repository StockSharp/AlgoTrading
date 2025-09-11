# Consecutive Bars Above MA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Short-only strategy that counts consecutive closes above a moving average and shorts on breakouts above the previous high. Exits when price falls below the prior low. Optional 200 EMA filter enforces downtrend.

## Details

- **Entry Criteria**: threshold of consecutive closes above MA and close > previous high
- **Long/Short**: Short
- **Exit Criteria**: close below previous low
- **Stops**: No
- **Default Values**:
  - `Threshold` = 3
  - `MaType` = SMA
  - `MaLength` = 5
  - `EmaPeriod` = 200
- **Filters**:
  - Category: Pattern
  - Direction: Short
  - Indicators: MA, EMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
