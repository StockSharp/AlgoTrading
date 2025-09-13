# TSI WPR Cross Strategy

This strategy trades based on the crossover of the True Strength Index (TSI) calculated from the Williams %R oscillator.
When the TSI crosses above its smoothed signal line the strategy enters a long position. When the TSI crosses below the signal line it enters a short position.

## Parameters
- **Candle Type**: Timeframe of candles used for calculation.
- **Williams %R Period**: Number of bars for the Williams %R indicator.
- **Short Length**: Short EMA length used in the TSI calculation.
- **Long Length**: Long EMA length used in the TSI calculation.
- **Signal Length**: EMA length applied to TSI to form the signal line.

## Trading Rules
1. Calculate Williams %R value of each finished candle.
2. Feed this value into the True Strength Index indicator.
3. Smooth the TSI with an EMA to obtain the signal line.
4. **Buy** when TSI crosses above the signal line.
5. **Sell** when TSI crosses below the signal line.
6. Existing positions in the opposite direction are closed on a new signal.

## Notes
- The strategy uses high-level API with automatic candle subscriptions.
- StartProtection is launched at startup for basic risk management.
- Chart areas are created to visualize TSI, its signal line and executed trades.
