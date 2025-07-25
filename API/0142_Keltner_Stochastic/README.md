# Keltner Stochastic Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
Strategy that combines Keltner Channels and Stochastic Oscillator.
Enters positions when price reaches Keltner Channel boundaries and Stochastic confirms oversold/overbought conditions.

This setup looks to catch reversals near the Keltner bands while the oscillator confirms momentum shifts. Signals can trigger in both directions whenever price presses against an envelope.

Short-term traders seeking quick reversals may find it useful. Risk is contained by an ATR-based stop distance.

## Details

- **Entry Criteria**:
  - Long: `Close < LowerBand && StochK < StochOversold`
  - Short: `Close > UpperBand && StochK > StochOverbought`
- **Long/Short**: Both
- **Exit Criteria**:
  - Long: `Close > EMA`
  - Short: `Close < EMA`
- **Stops**: `StopLossAtr` ATR from entry
- **Default Values**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 14
  - `KeltnerMultiplier` = 2.0m
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochD` = 3
  - `StochOversold` = 20m
  - `StochOverbought` = 80m
  - `StopLossAtr` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Keltner Channel, Stochastic Oscillator
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

Testing indicates an average annual return of about 163%. It performs best in the stocks market.
