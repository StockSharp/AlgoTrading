# Buy Sell Grid Strategy

## Overview

This strategy implements a simple grid approach that always keeps one long and one short position open. When the market moves enough to hit the take profit of one side, the opposite side is closed as well and the next grid level is opened with a larger volume. The volume grows geometrically according to the `VolumeMultiplier` parameter.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `TakeProfitPoints` | Take profit distance measured in price steps. |
| `InitialVolume` | Volume used for the first pair of orders. |
| `VolumeMultiplier` | Multiplier applied to the volume for each new grid level. |
| `MaxTrades` | Maximum number of grid levels allowed. |
| `CandleType` | Candle data type used to trigger the strategy logic. |

## Trading Logic

1. **Start** – The strategy subscribes to the specified candle series and opens the first pair of buy and sell market orders.
2. **Monitoring** – On each finished candle the last price is checked against the entry prices. If the profit target for one side is reached, both positions are closed.
3. **Grid Progression** – After closing all positions the next grid level is opened with volume multiplied by `VolumeMultiplier`.
4. **Limits** – The process repeats until `MaxTrades` levels are opened.

The strategy does not use any indicators or complex calculations which makes it suitable for demonstration of order management and position handling within StockSharp.

## Notes

- All comments in the code are written in English as required.
- The strategy uses the high-level API with `SubscribeCandles` for market data.

