# Williams %R Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy buys when Williams %R drops into deep oversold territory and exits on breakout or overbought levels.

## Details

- **Entry Criteria**: %R < -90
- **Long/Short**: Long only
- **Exit Criteria**: close > previous high or %R > -30
- **Stops**: No
- **Default Values**:
  - `LookbackPeriod` = 2
- **Filters**:
  - Category: Oscillator
  - Direction: Long
  - Indicators: WilliamsR
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

