# Color Zero Lag MA Strategy

This strategy uses a zero lag moving average (ZLMA) to detect trend reversals. It opens long positions when the ZLMA turns upward and opens short positions when the ZLMA turns downward. Existing positions are closed when the indicator slope reverses.

## Parameters

- **Length**: Period of the zero lag moving average.
- **Candle Type**: Timeframe for candles used by the strategy.
- **Open Buy**: Enable opening long positions.
- **Open Sell**: Enable opening short positions.
- **Close Buy**: Close long positions when ZLMA turns down.
- **Close Sell**: Close short positions when ZLMA turns up.

## Logic

1. Subscribe to candles of the selected timeframe.
2. Calculate the zero lag moving average.
3. Track the last two ZLMA values to determine slope direction.
4. If the slope changes from down to up, close short positions and open a long position.
5. If the slope changes from up to down, close long positions and open a short position.

This simple approach follows the color change of the zero lag moving average to capture potential trend reversals.
