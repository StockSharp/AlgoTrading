# The MasterMind 2 Strategy

## Overview

This strategy combines the **Stochastic Oscillator** and **Williams %R** to identify extreme oversold and overbought conditions.
A long position is opened when the Stochastic signal line drops below **3** and Williams %R is lower than **-99.9**.
A short position is opened when the Stochastic signal line rises above **97** and Williams %R is higher than **-0.1**.

Risk control includes an initial stop loss and take profit, a trailing stop with adjustable step, and an optional break-even trigger that moves the stop to the entry price after sufficient profit.

## Parameters

- `LotSize` - trade volume in contracts.
- `StochasticPeriod` - period for the Stochastic Oscillator.
- `StochasticK` - smoothing of the %K line.
- `StochasticD` - smoothing of the %D (signal) line.
- `WilliamsRPeriod` - period for Williams %R.
- `StopLossPoints` - initial stop loss in price points.
- `TakeProfitPoints` - initial take profit in price points.
- `TrailingStopPoints` - trailing stop distance in points.
- `TrailingStepPoints` - minimum favorable movement before the trailing stop is updated.
- `BreakEvenPoints` - distance in points to move stop to break-even.
- `CandleType` - type and timeframe of candles used for calculations.

## Trading Logic

1. **Entry Signals**
   - **Buy** when Stochastic signal < 3 and Williams %R < -99.9.
   - **Sell** when Stochastic signal > 97 and Williams %R > -0.1.
2. **Exit Signals**
   - Opposite entry signals close existing positions.
   - Stop loss, take profit, break-even and trailing stop are applied on every candle.

## Notes

- Works on any instrument that supports the required indicators.
- Designed for educational purposes and further experimentation.
