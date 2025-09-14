# Trix Candle Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades reversals based on the Trix Candle indicator, which applies a triple exponential moving average to candle open and close prices and colors each candle depending on whether the smoothed close is above or below the smoothed open.

## Details

- **Entry Criteria**:
  - **Long**: previous candle bullish (color 2) and current candle color < 2
  - **Short**: previous candle bearish (color 0) and current candle color > 0
- **Long/Short**: Long and Short
- **Exit Criteria**:
  - Long: previous candle bearish (color 0)
  - Short: previous candle bullish (color 2)
- **Stops**: No
- **Default Values**:
  - `TRIX Period` = 14
  - `Candle Type` = 4h
  - `Allow Buy Open` = true
  - `Allow Sell Open` = true
  - `Allow Buy Close` = true
  - `Allow Sell Close` = true
- **Filters**:
  - Category: Reversal
  - Direction: Both
  - Indicators: Triple Exponential Moving Average
  - Stops: No
  - Complexity: Low
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
