# Combo Strategy 123 Reversal & Fractal Chaos Bands Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy combining a 123 reversal pattern with Fractal Chaos Bands breakout.
Long trades occur when a bullish 123 reversal forms and price closes above the upper fractal band.
Short trades occur when a bearish 123 reversal aligns with a close below the lower fractal band.

## Details

- **Entry Criteria**:
  - Long: Reversal123 long signal and close above upper fractal band.
  - Short: Reversal123 short signal and close below lower fractal band.
- **Long/Short**: Both
- **Exit Criteria**: Opposite signal
- **Stops**: No
- **Default Values**:
  - `Length` = 15
  - `KSmoothing` = 1
  - `DLength` = 3
  - `Level` = 50m
  - `Pattern` = 1
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Pattern & breakout
  - Direction: Both
  - Indicators: Stochastic Oscillator, Fractal Chaos Bands
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
