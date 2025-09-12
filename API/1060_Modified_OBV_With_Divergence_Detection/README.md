# Modified OBV with Divergence Detection
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy smooths On-Balance Volume (OBV) with a selectable moving average and generates a signal line. Trades occur when the smoothed OBV crosses the signal. Additionally, the strategy logs regular and hidden divergences between price and OBV using fractal detection.

## Details

- **Entry Criteria**: OBV-M crosses above/below signal line.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite crossover.
- **Stops**: No.
- **Default Values**:
  - `MaType` = Exponential
  - `ObvMaLength` = 7
  - `SignalLength` = 10
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Divergence
  - Direction: Both
  - Indicators: OBV, MA
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: Yes
  - Risk Level: Medium
