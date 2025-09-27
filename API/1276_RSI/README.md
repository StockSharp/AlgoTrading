# RSI Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Simple strategy based on the Relative Strength Index. Buys when RSI crosses above the oversold level and sells when it crosses below the overbought level.

## Details

- **Entry Criteria**:
  - Long: RSI crossing above `OverSold`
  - Short: RSI crossing below `OverBought`
- **Long/Short**: Both
- **Exit Criteria**:
  - Opposite signal
- **Stops**: No
- **Default Values**:
  - `RsiLength` = 14
  - `OverSold` = 25m
  - `OverBought` = 75m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Oscillator
  - Direction: Both
  - Indicators: RSI
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Low
