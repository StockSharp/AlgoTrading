# Bullish Reversal Bar Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Implementation of strategy - Bullish Reversal Bar. Enters long when a bullish reversal bar forms below Alligator lines and price breaks above the bar high. Optional filters can enable Awesome Oscillator and Market Facilitation Index squat bars.

The setup looks for a new low that closes in the upper half of the candle while trend turns bullish. The confirmation comes when price exceeds the bar high.

## Details

- **Entry Criteria**:
  - Long: `bullish reversal bar && close > confirmation level`
- **Long/Short**: Long only
- **Exit Criteria**:
  - Stop-loss at bar low or trend turns down
- **Stops**: Bar low stored in `_stopLoss`
- **Default Values**:
  - `EnableAo` = false
  - `EnableMfi` = false
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Trend following
  - Direction: Long
  - Indicators: Alligator, Awesome Oscillator, Market Facilitation Index
  - Stops: Yes
  - Complexity: Advanced
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

