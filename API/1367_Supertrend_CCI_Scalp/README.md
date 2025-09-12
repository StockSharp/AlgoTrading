[Русский](README_ru.md) | [中文](README_cn.md)

Supertrend & CCI Strategy Scalp uses two Supertrend lines and a smoothed CCI to capture short-term reversals.
It buys when the first Supertrend is above price, the second is below price, and the smoothed CCI is below -100. The short logic mirrors this setup.

## Details

- **Entry Criteria**: Supertrend1 above price, Supertrend2 below price, smoothed CCI < -100 (long); opposite for short
- **Long/Short**: Both
- **Exit Criteria**: Opposite Supertrend alignment or CCI crossing ±100
- **Stops**: No
- **Default Values**:
  - `AtrLength1` = 14
  - `Factor1` = 3
  - `AtrLength2` = 14
  - `Factor2` = 6
  - `CciLength` = 20
  - `SmoothingLength` = 5
  - `MaType` = MovingAverageTypeEnum.Simple
  - `CciLevel` = 100
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Supertrend, CCI, Moving Average
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

