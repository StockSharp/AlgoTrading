# RSI Strategy with Adjustable RSI and Stop-Loss
[Русский](README_ru.md) | [中文](README_cn.md)

Buys when the RSI value drops below a threshold and closes the long position when the price breaks above the previous candle's high. A percent-based stop loss protects each trade.

## Details

- **Entry Criteria**:
  - Long: RSI below `RsiThreshold`
- **Long/Short**: Long
- **Exit Criteria**:
  - Close price above previous candle high
  - Stop loss
- **Stops**: Yes
- **Default Values**:
  - `RsiLength` = 8
  - `RsiThreshold` = 28m
  - `StopLossPercent` = 5m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Oscillator
  - Direction: Long
  - Indicators: RSI
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Short-term
  - Seasonality: None
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

