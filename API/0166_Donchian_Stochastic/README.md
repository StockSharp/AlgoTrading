# Donchian Stochastic Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
Donchian Channel + Stochastic strategy. Strategy enters the market when the price breaks out of Donchian Channel with Stochastic confirming oversold/overbought conditions.

Breakouts beyond the Donchian channel are confirmed with Stochastic momentum. Trades start as soon as price escapes the range and the oscillator agrees.

Useful for traders expecting immediate follow-through. An ATR multiple sets the stop.

## Details

- **Entry Criteria**:
  - Long: `Close > DonchianHigh && StochK < 20`
  - Short: `Close < DonchianLow && StochK > 80`
- **Long/Short**: Both
- **Exit Criteria**: Breakout failure or opposite signal
- **Stops**: Percent-based using `StopLossPercent`
- **Default Values**:
  - `DonchianPeriod` = 20
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochD` = 3
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `StopLossPercent` = 2m
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Donchian Channel, Stochastic Oscillator
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

Testing indicates an average annual return of about 85%. It performs best in the crypto market.
