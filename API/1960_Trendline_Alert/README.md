# Trendline Alert Strategy

This strategy monitors two user-defined trendlines and reacts when the price breaks them. The upper and lower lines represent resistance and support levels. When the closing price crosses above the upper line, a long position is opened. When the price falls below the lower line, a short position is opened. Optional trailing stop logic protects opened positions by moving the stop level in the trade direction.

## Parameters

- `Breakout Points` – additional points added to the trendline levels to define the breakout threshold.
- `Upper Line` – price level for the bullish breakout.
- `Lower Line` – price level for the bearish breakout.
- `Start Hour` – trading start time in hours.
- `End Hour` – trading end time in hours.
- `Use Trailing Stop` – enables trailing stop management.
- `Trailing Stop Points` – distance in points for the trailing stop.
- `Candle Type` – candle timeframe used for analysis.

## How It Works

1. The strategy subscribes to the selected candle series.
2. For each finished candle it verifies that the time is within the specified trading window.
3. A breakout is detected when the candle close crosses above the upper line or below the lower line, adjusted by the breakout points threshold.
4. When a breakout occurs a market order is sent in the breakout direction if there is no existing position.
5. If trailing stop is enabled the stop level follows the price until it is triggered.

## Notes

- The strategy is a simplified conversion of the original MetaTrader TrendlineAlert expert advisor. Manual drawing of trendlines is replaced with fixed price levels defined by parameters.
- No orders are placed outside the specified trading hours.
