# Moving Averages Strategy

## Overview
This strategy replicates the classic moving-average crossover expert advisor from MQL. It uses high-level StockSharp APIs to monitor two simple moving averages calculated from the selected candle series. Signals are generated when the fast average crosses the slow one, and the strategy can optionally close an active position when the opposite crossover occurs.

## Trading Logic
- Subscribe to the configured candle type and calculate fast and slow SMA values on every completed candle.
- Detect a bullish crossover when the fast SMA moves from below to above the slow SMA. If no position is active, open a long position with the specified volume.
- Detect a bearish crossover when the fast SMA moves from above to below the slow SMA. If no position is active, open a short position with the specified volume.
- Optionally close an existing position immediately when the opposite crossover is detected, mirroring the "Close on Opposite Signal" switch from the original script.

## Risk Management
- Apply a fixed stop loss and take profit expressed in price points. Both levels are recalculated for every new entry.
- Move the protective stop to break-even after the price travels by the configured trigger distance and keep an additional offset as locked-in profit.
- Activate a trailing stop once the position gains the defined start distance. The stop is shifted using the most favorable candle price while never moving against the trade.

## Parameters
- **Fast MA Period** – length of the fast SMA used for crossover detection.
- **Slow MA Period** – length of the slow SMA used for crossover detection.
- **Trade Volume** – order size in lots.
- **Stop Loss (points)** – distance in instrument points for the initial stop loss.
- **Take Profit (points)** – distance in instrument points for the initial take profit.
- **Break-even Trigger** – profit distance that activates moving the stop to break-even.
- **Break-even Offset** – additional points kept as profit after break-even is activated.
- **Trailing Start** – profit distance required before enabling the trailing stop.
- **Trailing Distance** – distance maintained between price and the trailing stop.
- **Close On Opposite** – whether to close an active trade when an opposite crossover appears.
- **Candle Type** – candle series used for indicator calculations.

## Usage Notes
- Ensure the strategy is attached to a security with a valid `PriceStep`. When the step is unavailable, a value of 1 is used.
- Trailing and break-even management operate on completed candles, matching the behavior of the original EA that reacts on bar close.
- Optimize the moving-average lengths and risk settings to adapt the system to different markets or timeframes.
