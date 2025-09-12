# Liquid Pulse Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Detects high volume spikes confirmed by MACD and ADX. ATR defines stop and take profit with daily trade limit.

## Details

- **Entry Criteria**:
  - Long: volume spike, MACD crosses above signal, +DI > -DI, ADX >= threshold
  - Short: volume spike, MACD crosses below signal, -DI > +DI, ADX >= threshold
- **Long/Short**: Both
- **Exit Criteria**: ATR-based stop or take profit
- **Stops**: ATR multiples
- **Default Values**:
  - `VolumeSensitivity` = Medium
  - `MacdSpeed` = Medium
  - `DailyTradeLimit` = 20
  - `AtrPeriod` = 9
  - `AdxTrendThreshold` = 41
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: MACD, ADX, ATR, Volume
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
