# Up3x1 Strategy

The Up3x1 strategy uses three simple moving averages to capture trend changes:

- **Fast SMA**: reacts quickly to price changes.
- **Middle SMA**: provides additional confirmation of the trend.
- **Slow SMA**: defines the global direction of the market.

### Entry Rules

- **Buy** when the fast SMA crosses above the middle SMA and both are below the slow SMA.
- **Sell** when the fast SMA crosses below the middle SMA and both are above the slow SMA.

### Exit Rules

- A fixed take profit and stop loss are applied to each position.
- An optional trailing stop can protect profits by following the price after entry.

### Parameters

- `Volume` – order size.
- `TakeProfit` – profit target in price units.
- `StopLoss` – loss limit in price units.
- `TrailingStop` – trailing distance; set to 0 to disable.
- `FastPeriod`, `MiddlePeriod`, `SlowPeriod` – lengths of the moving averages.
- `CandleType` – candle timeframe used for calculations.

The strategy is designed for demonstration and can be further customized for specific trading instruments or conditions.
