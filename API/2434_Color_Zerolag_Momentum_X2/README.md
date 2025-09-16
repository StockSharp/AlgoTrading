# Color Zerolag Momentum X2 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Dual timeframe momentum strategy using a zero lag moving average cross. The higher timeframe defines trend direction, while the lower timeframe triggers entries when momentum crosses its zero lag average in the direction of the trend.

## Details

- **Entry Criteria**: momentum crosses its zero lag average in trend direction
- **Long/Short**: Both
- **Exit Criteria**: opposite cross or trend reversal
- **Stops**: No
- **Default Values**:
  - `TrendCandleType` = 6h
  - `TrendMomentumPeriod` = 34
  - `TrendMaLength` = 15
  - `SignalCandleType` = 30m
  - `SignalMomentumPeriod` = 34
  - `SignalMaLength` = 15
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Momentum, ZeroLagEMA
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Multi-timeframe
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
