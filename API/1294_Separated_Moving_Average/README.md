# Separated Moving Average
[Русский](README_ru.md) | [中文](README_cn.md)

Builds separate moving averages for bullish and bearish closes. A long position opens when the bullish average rises above the bearish one, and a short position opens on the reverse cross. The strategy supports SMA, EMA, or HMA and can operate on Heikin Ashi prices.

## Details

- **Entry Criteria**: Bullish average crossing above bearish average.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite cross.
- **Stops**: No.
- **Default Values**:
  - `MaType` = MaType.SMA
  - `Length` = 20
  - `UseHeikinAshi` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: SMA, EMA, HMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

