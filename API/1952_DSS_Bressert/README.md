# DSS Bressert Strategy

This strategy uses the Double Smoothed Stochastic (DSS) Bressert indicator. Two lines are calculated:

- **DSS line** – stochastic value smoothed twice with exponential moving average.
- **MIT line** – intermediate value after the first smoothing.

A trade is opened when these lines cross:

- Buy when the DSS line crosses below the MIT line after being above it.
- Sell when the MIT line crosses below the DSS line after being above it.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `EmaPeriod` | EMA smoothing period (default: 8) |
| `StoPeriod` | Stochastic calculation period (default: 13) |
| `TakeProfitPercent` | Take profit percentage for protective orders (default: 2) |
| `StopLossPercent` | Stop loss percentage for protective orders (default: 1) |
| `CandleType` | Timeframe used for calculations (default: 4 hours) |

## Notes

- Strategy works on closed candles only.
- Protection uses percentage based stop loss and take profit.
