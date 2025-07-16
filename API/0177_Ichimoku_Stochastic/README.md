# Ichimoku Stochastic Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
Strategy based on Ichimoku Cloud and Stochastic Oscillator indicators.
Enters long when price is above Kumo (cloud), Tenkan > Kijun, and Stochastic is oversold (< 20) Enters short when price is below Kumo, Tenkan < Kijun, and Stochastic is overbought (> 80)

Ichimoku outlines trend and support levels while Stochastic times the entry on pullbacks. Trades open when the oscillator resets within the prevailing cloud direction.

Traders who favor structured indicators may find it practical. ATR stops cover abrupt reversals.

## Details

- **Entry Criteria**:
  - Long: `Price > Cloud && StochK < 20`
  - Short: `Price < Cloud && StochK > 80`
- **Long/Short**: Both
- **Exit Criteria**:
  - Cloud breakout in opposite direction
- **Stops**: Uses Ichimoku cloud boundaries
- **Default Values**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouPeriod` = 52
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochD` = 3
  - `CandleType` = TimeSpan.FromMinutes(30).TimeFrame()
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Ichimoku Cloud, Stochastic Oscillator
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
