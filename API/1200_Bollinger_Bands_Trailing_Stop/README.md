# Bollinger Bands with Trailing Stop
[Русский](README_ru.md) | [中文](README_cn.md)

Enters long when price closes above the upper Bollinger Band. 
Exits when price falls below the lower band or a trailing stop based on ATR is triggered.

## Details

- **Entry Criteria**: Close above upper band.
- **Long/Short**: Long only.
- **Exit Criteria**: Close below lower band or trailing stop hit.
- **Stops**: Trailing stop.
- **Default Values**:
  - `BbLength` = 20
  - `BbDeviation` = 2m
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
  - `MaType` = MovingAverageTypeEnum.Simple
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Long
  - Indicators: Bollinger Bands, ATR
  - Stops: Yes
  - Complexity: Beginner
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
