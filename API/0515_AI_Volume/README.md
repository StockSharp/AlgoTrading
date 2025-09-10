# AI Volume Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

AI Volume Strategy hunts for sudden participation bursts. A volume spike occurs when current volume exceeds its EMA by a given multiplier. If the spike aligns with the 50-period price EMA and the candle color, the strategy enters in that direction. Each trade is closed after a fixed number of bars.

## Details

- **Entry Criteria**: Volume > VolumeEMA * VolumeMultiplier and price above/below 50 EMA with matching candle color.
- **Long/Short**: Both directions.
- **Exit Criteria**: Position closed after `ExitBars` candles.
- **Stops**: None.
- **Default Values**:
  - `VolumeEmaLength` = 20
  - `VolumeMultiplier` = 2.0
  - `ExitBars` = 5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Volume breakout
  - Direction: Both
  - Indicators: EMA, Volume EMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

