# Stochastic Diff Strategy

This strategy trades based on the difference between the Stochastic oscillator's %K and %D lines. The difference is smoothed with an exponential moving average to reduce noise. A long position opens when the smoothed difference forms a local trough and turns upward. A short position opens when the smoothed difference forms a local peak and turns downward.

## How It Works

1. Calculate Stochastic %K and %D with user-defined periods.
2. Compute the difference `%K - %D` and smooth it with an EMA.
3. Detect turning points in the smoothed difference:
   - If the value was decreasing and then rises, enter a long position.
   - If the value was increasing and then falls, enter a short position.
4. Apply optional stop-loss and take-profit protections in percent.

## Parameters

| Name | Description |
| --- | --- |
| Candle Type | Candle type used for calculations |
| %K Period | Period for the %K line |
| %D Period | Period for the %D line |
| Slowing | Additional smoothing of %K |
| Smoothing Length | EMA length for the difference |
| Stop Loss % | Stop-loss size in percent |
| Take Profit % | Take-profit size in percent |

## Notes

- Works on any instrument and timeframe supported by the data feed.
- Designed for educational purposes to demonstrate indicator-based entry signals.
