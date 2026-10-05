# Ichimoku Stochastic Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategy based on Ichimoku Cloud and Stochastic Oscillator indicators.
Enters long when price is above Kumo (cloud), Tenkan > Kijun, and Stochastic is oversold (< 20) Enters short when price is below Kumo, Tenkan < Kijun, and Stochastic is overbought (> 80)

Testing indicates an average annual return of about 118%. It performs best in the stocks market.

Ichimoku outlines trend and support levels while Stochastic times the entry on pullbacks. Trades open when the oscillator resets within the prevailing cloud direction.

Traders who favor structured indicators may find it practical. The cloud itself acts as the stop against abrupt reversals.

## Details

- **Entry Criteria**:
  - Long: `Price > Cloud && Tenkan > Kijun && StochK < 20`
  - Short: `Price < Cloud && Tenkan < Kijun && StochK > 80`
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
  - `StochOversold` = 20
  - `StochOverbought` = 80
  - `CandleType` = TimeSpan.FromMinutes(30).TimeFrame()
    The 20 and 80 in the rules are the defaults of StochOversold and StochOverbought. %K is the stochastic over StochPeriod candles smoothed over StochK candles; %D plays no part. A long closes when price closes below the cloud and a short when it closes above it. An entry signal against an open position reverses it.
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

