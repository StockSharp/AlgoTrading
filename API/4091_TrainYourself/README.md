# TrainYourself Strategy

This strategy is a StockSharp port of the MetaTrader 4 expert advisor **TrainYourself-V1_1-1**. It recreates the channel-building and breakout logic while replacing the graphical buttons of the MT4 script with explicit method calls. The algorithm continuously rebuilds a Donchian-style price channel and fires a trade once price escapes the channel after first consolidating inside it.

## Trading logic

1. **Channel construction**
   - A `DonchianChannels` indicator with `ChannelLength` periods is evaluated on every finished candle of the selected `CandleType`.
   - The raw upper and lower bands are expanded with an extra MetaTrader-like buffer: `BufferPoints` multiplied by the instrument `PriceStep`. This reproduces the original script that initially placed the trendlines 50 points away from the current bid/ask before sliding them over recent highs and lows.
   - The resulting `UpperBand`/`LowerBand` values are exposed as read-only properties so they can be displayed in custom dashboards.

2. **Arming condition**
   - The breakout engine stays disarmed while a position is open or when `EnableTrendTrade` is false.
   - When there is no position, price must close inside the channel with an additional margin of `ActivationPoints` * `PriceStep` from both boundaries. Only then `_isArmed` becomes `true`, mimicking the MetaTrader flag `q=1` that was set when price moved back into the channel.

3. **Breakout execution**
   - Once armed, a close at or above the `UpperBand` places a market buy order (if `AllowBuyOpen` is enabled). A close at or below the `LowerBand` places a market sell order (respecting `AllowSellOpen`).
   - After an order is placed the strategy disarms itself until price re-enters the channel without any open position.

4. **Risk management**
   - `StartProtection` configures automatic protective orders. Distances are calculated by multiplying `TakeProfitPoints` and `StopLossPoints` with the current `PriceStep`. If the broker does not report a step, a fallback of `0.0001` is used, matching the MetaTrader `Point` behaviour.

5. **Manual controls**
   - The MT4 labels (`BUY_TRIANGLE`, `SELL_TRIANGLE`, `CLOSE_ORDER`) are replaced by three public methods: `TriggerManualBuy()`, `TriggerManualSell()`, and `ClosePositionManually()`. They honour `AllowBuyOpen`/`AllowSellOpen`, check connection status via `IsFormedAndOnlineAndAllowTrading()`, and also disarm the breakout logic so that manual trades do not immediately trigger automated entries.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `CandleType` | `30m` time frame | Primary candle subscription used for all calculations. |
| `ChannelLength` | `20` | Number of candles analysed by the Donchian channel. |
| `BufferPoints` | `50` | Extra MetaTrader points added around the last close before finalising the channel. |
| `ActivationPoints` | `2` | Margin (in points) that price must keep away from the channel edges before a breakout can be armed. |
| `StopLossPoints` | `100` | Stop-loss distance in points; converted to absolute price by multiplying with `PriceStep`. |
| `TakeProfitPoints` | `100` | Take-profit distance in points; converted to absolute price using `PriceStep`. |
| `EnableTrendTrade` | `true` | Enables automatic breakout trading. When `false` only the manual helper methods can open/close positions. |
| `Volume` | `1` | Order size for both automatic and manual trades. |

## Usage notes

- The original expert advisor required dragging on-chart icons to (re)build trendlines. In StockSharp the channel is rebuilt automatically on every candle, so no manual refresh is necessary.
- Because the strategy exposes `UpperBand`, `LowerBand`, and `IsArmed`, dashboards or UI widgets can replicate the original visual feedback without relying on chart objects.
- Stop-loss and take-profit levels are optional. Set the corresponding parameters to `0` to disable the protective orders, mirroring the MetaTrader behaviour where the modification routines were skipped when the extern value was zero.
- Manual entries respect the same `Volume` parameter and automatically benefit from the configured protective distances.
- To reset the breakout state manually, call `ClosePositionManually()` (which also clears `IsArmed`) or wait for price to re-enter the channel so that the arming condition is satisfied again.
