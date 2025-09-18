# The MasterMind Strategy

Strategy using Stochastic oscillator and Williams %R to capture extreme oversold and overbought conditions.

## Overview
The strategy monitors two momentum indicators:
- **Stochastic Oscillator** with base length 100 and smoothing 3/3.
- **Williams %R** with length 100.

A long position is opened when the Stochastic %D value drops below 3 while Williams %R is under -99.9, indicating an oversold market.
A short position is opened when Stochastic %D rises above 97 and Williams %R climbs above -0.1, signalling an overbought market.

After entering a trade, the algorithm manages risk through stop loss, take profit, trailing stop and optional break-even movement.

## Parameters
- `StochasticLength` – period for Stochastic and Williams %R calculations.
- `StopLoss` – distance from entry price for stop loss in points.
- `TakeProfit` – take profit distance in points.
- `TrailingStop` – activation distance for trailing in points.
- `TrailingStep` – step of trailing stop in points.
- `BreakEven` – profit in points at which the stop is moved to entry.
- `CandleType` – candle time frame for strategy calculations.

## Indicators
- `StochasticOscillator`
- `WilliamsR`

## Trading Rules
1. **Buy** when `%D < 3` and `Williams %R < -99.9`.  
2. **Sell** when `%D > 97` and `Williams %R > -0.1`.  
3. After entry, apply stop loss and take profit.  
4. Move stop to break-even when price advances by `BreakEven`.  
5. Activate trailing stop after price moves by `TrailingStop`, shifting by `TrailingStep`.

## Notes
The strategy uses the high-level API of StockSharp and is intended as an educational example.
