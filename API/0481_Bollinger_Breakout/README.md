# 4H Bollinger Breakout Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The 4H Bollinger Breakout strategy trades Bollinger Band breakouts on the four-hour chart. Long positions are opened when price crosses above the lower band with volume and trend confirmation. Short positions are taken when price crosses below the upper band and RSI is below a threshold.

## Details

- **Entry Criteria**:
  - **Long**: Close crosses above lower band, volume above its SMA and price above trend SMA.
  - **Short**: Close crosses below upper band, volume above its SMA, price below trend SMA, RSI < 85.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Close crosses above upper band.
  - **Short**: Close crosses below lower band.
- **Stops**: None.
- **Default Values**:
  - `BollingerLength` = 20
  - `BollingerMultiplier` = 1.8
  - `VolumeLength` = 20
  - `TrendLength` = 80
  - `RsiLength` = 14
  - `UseLongSignals` = True
  - `UseShortSignals` = True
- **Filters**:
  - Category: Trend breakout
  - Direction: Both
  - Indicators: Bollinger Bands, Volume SMA, Trend SMA, RSI
  - Stops: None
  - Complexity: Medium
  - Timeframe: 4H
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
