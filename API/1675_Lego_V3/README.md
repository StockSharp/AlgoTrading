# Lego V3 Strategy

This strategy is a port of the MQL4 "Lego_v3" expert advisor.  
It combines several classic indicators to generate entries and exits:

- **Moving Averages** – fast and slow SMA to detect trend direction.
- **Stochastic Oscillator** – %K and %D values define oversold and overbought zones.
- **Awesome Oscillator** – confirms momentum alignment with the trend.
- **Average True Range** – determines stop-loss and take-profit distances.

A long position is opened when the fast MA crosses above the slow MA, the Stochastic %K is below the buy level, and the Awesome Oscillator is positive.  
Short positions occur under opposite conditions. ATR is used once at the beginning to start protective stop management.

## Parameters

- `FastMaPeriod` – period for the fast moving average.
- `SlowMaPeriod` – period for the slow moving average.
- `StochK` – %K period for the Stochastic oscillator.
- `StochD` – %D period for the Stochastic oscillator.
- `StochBuy` – buy zone threshold for %K.
- `StochSell` – sell zone threshold for %K.
- `AtrPeriod` – period for ATR calculation.
- `AtrMultiplier` – multiplier applied to ATR for stop levels.
- `CandleType` – time frame of processed candles.
