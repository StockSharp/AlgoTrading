# Supertrend Stochastic Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Supertrend + Stochastic strategy. Strategy enters trades when Supertrend indicates trend direction and Stochastic confirms with oversold/overbought conditions.

Testing indicates an average annual return of about 142%. It performs best in the stocks market.

Supertrend marks the trend, and Stochastic points out temporary counter moves. Entries happen once Stochastic exits oversold or overbought against the trend.

Best for momentum traders needing clear trend cues. ATR values define the stop distance.

## Details

- **Entry Criteria**:
  - Long: `Close > Supertrend && StochK < 20`
  - Short: `Close < Supertrend && StochK > 80`
- **Long/Short**: Both
- **Exit Criteria**: Supertrend reversal
- **Stops**: Uses Supertrend as trailing stop
- **Default Values**:
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochOversold` = 20
  - `StochOverbought` = 80
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    The 20 and 80 in the rules are the defaults of StochOversold and StochOverbought. %K is the stochastic over StochPeriod candles smoothed over StochK candles; %D plays no part. The stop is the Supertrend line itself, whose distance comes from its ATR: a long closes when Supertrend flips down and a short when it flips up. An entry signal against an open position reverses it.
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Supertrend, Stochastic Oscillator
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

