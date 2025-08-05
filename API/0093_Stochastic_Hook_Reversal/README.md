# Stochastic Hook Reversal Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
The Stochastic Hook Reversal watches the %K line for a hook out of overbought or oversold territory.
After stretching to an extreme the oscillator often curls back, indicating momentum is waning.

Testing indicates an average annual return of about 166%. It performs best in the stocks market.

The system enters long when %K turns up from below twenty as price presses a new low.
It sells short when the oscillator hooks down from above eighty during a final push higher.

Positions use a small percent stop and close when the stochastic hooks the other way or the stop is reached.

## Details

- **Entry Criteria**: indicator signal
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filters**:
  - Category: Reversal
  - Direction: Both
  - Indicators: Stochastic
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

