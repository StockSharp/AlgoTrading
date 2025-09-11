# CVD Divergence
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy combines cumulative volume delta (CVD) divergence with Hull Moving Averages, RSI, MACD and a volume filter. A trade opens when trend, momentum and volume agree and CVD shows divergence or continues in trade direction. Positions close on opposite signals or indicator cross.

## Details

- **Entry Criteria**: Trend alignment by HMA, RSI and MACD confirmation, high volume and CVD divergence/continuation.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or indicator cross.
- **Stops**: No explicit stops.
- **Default Values**:
  - `HmaFastLength` = 20
  - `HmaSlowLength` = 50
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `VolumeMaLength` = 20
  - `VolumeMultiplier` = 1.5m
  - `CvdLength` = 14
  - `DivergenceLookback` = 5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Divergence
  - Direction: Both
  - Indicators: HMA, RSI, MACD, Volume
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: Yes
  - Risk Level: Medium
