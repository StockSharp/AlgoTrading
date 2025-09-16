# ZPF Volume Filter
[Русский](README_ru.md) | [中文](README_cn.md)

ZPF Volume Filter combines two moving averages with a volume average. The indicator value is the volume-smoothed difference between a fast and a slow moving average. When this value crosses above zero, bullish pressure is assumed; a cross below zero signals bearish pressure.

The strategy trades both directions. Entries occur when the ZPF indicator crosses the zero line. Positions are closed when an opposite cross happens.

## Details

- **Entry Criteria**: ZPF crosses above or below zero.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite zero-line cross.
- **Stops**: No.
- **Default Values**:
  - `Length` = 12
  - `CandleType` = TimeSpan.FromHours(4)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Moving Average, Volume
  - Stops: No
  - Complexity: Basic
  - Timeframe: Swing
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

