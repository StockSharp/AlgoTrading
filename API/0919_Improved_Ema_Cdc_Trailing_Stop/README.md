# Improved EMA & CDC Trailing Stop Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Combines EMA trend filter, MACD confirmation and an ATR-based CDC trailing stop.

## Details

- **Entry Criteria**:
  - **Long**: price > EMA60, EMA60 > EMA90, MACD line > signal line.
  - **Short**: price < EMA60, EMA60 < EMA90, MACD line < signal line.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Trailing stop or ATR-based profit target.
- **Stops**: Yes.
- **Default Values**:
  - `Ema60Period` = 60
  - `Ema90Period` = 90
  - `AtrPeriod` = 24
  - `Multiplier` = 4
  - `ProfitTargetMultiplier` = 2
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: EMA, MACD, ATR
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
