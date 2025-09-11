# Dema RSI Strategy

This strategy trades crossovers between the RSI of a double exponential moving average and its smoothed value. A long position opens when the RSI crosses above the smoothed line and a short position opens on the opposite crossover. Positions can be protected with take profit, trailing stop and an optional trading session filter.

## Parameters
- Candle Type
- MA length
- RSI length
- RSI smoothing length
- Take profit points
- Trail stop points
- Use session
- Session start
- Session end
