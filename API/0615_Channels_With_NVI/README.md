# Channels with NVI Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses Bollinger Bands or Keltner Channels combined with the Negative Volume Index (NVI). A long position opens when price closes below the lower band while NVI is above its EMA. The position closes when NVI falls below its EMA. Optional stop-loss and take-profit percentages are available.

## Details

- **Entry Criteria**:
  - **Long**: Close < lower band and NVI > NVI EMA.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - **Long**: NVI < NVI EMA.
- **Stops**: Optional, percent of entry price.
- **Default Values**:
  - `ChannelType` = "BB"
  - `ChannelLength` = 20
  - `ChannelMultiplier` = 2
  - `NviEmaLength` = 200
  - `EnableStopLoss` = false
  - `StopLossPercent` = 0
  - `EnableTakeProfit` = false
  - `TakeProfitPercent` = 0
- **Filters**:
  - Category: Channel
  - Direction: Long
  - Indicators: Bollinger Bands or Keltner Channels, EMA, NVI
  - Stops: Optional
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
