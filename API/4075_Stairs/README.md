# Stairs Strategy

The **Stairs Strategy** reproduces the behaviour of the original MetaTrader expert. It starts by placing symmetrical stop orders around the current ask price and then continuously rebuilds the grid around the most recent fill. Profits are accumulated in price steps (pips) without weighting by volume, exactly as in the source script. When a profit target is hit, the strategy liquidates all positions by market order, removes any pending stops, and resets the grid.

## Trading logic

1. When no positions are open, place a buy stop and a sell stop at a distance of `ChannelSteps / 2` price steps above and below the current ask price.
2. After the first stop order is filled, re-arm the grid around the executed price:
   - If there are fewer than two active stop orders, cancel the stale ones.
   - As long as the current bid price remains within half of the channel distance from the last entry, place a new buy stop and sell stop `ChannelSteps` away from the most recent fill.
   - When `AddLots` is enabled, increase the pending order volume by the base lot after each fill.
3. Maintain two running lists with all long and short entries in order to reproduce the hedged basket used by the MT4 version.
4. Compute the unrealised profit of the basket on every finished candle using the best bid for longs and the best ask for shorts. Distances are normalised by the instrument price step, mirroring the original point calculation.
5. Trigger a full liquidation when either threshold is exceeded:
   - `ProfitSteps` – profit produced by the current symbol only.
   - `CommonProfitSteps` – profit across the entire basket.
6. Liquidation sends market orders to close every long and short exposure separately. Pending stop orders are cancelled once the basket is flat.

> **Note**: The original expert attached stop-loss levels when registering pending orders. StockSharp does not support per-order protective levels through the high-level API, therefore the port closes trades exclusively through the profit-based logic described above.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `ChannelSteps` | Distance (in minimum price steps) between the symmetric stop orders. | `1000` |
| `ProfitSteps` | Profit threshold (in steps) required to close the local basket. | `1500` |
| `CommonProfitSteps` | Global profit threshold (in steps) that forces a full liquidation. | `1000` |
| `AddLots` | When enabled, increase the next pending order volume by the base lot after each fill. | `true` |
| `BaseVolume` | Volume used for the very first pair of stop orders. | `0.1m` |
| `CandleType` | Timeframe used for candle subscriptions and trade management. | `1 minute` |

## Implementation notes

- Uses the StockSharp high-level API with `SubscribeCandles()` and `Bind()` to process finished candles only.
- Tracks individual entries inside `OnOwnTradeReceived` so the profit calculation can mimic the hedging logic of the MQL version.
- Profit thresholds operate on pure price-step distances, without multiplying by the executed volume, matching the way the MT4 expert summed pips.
- All stop orders are created through `BuyStop` and `SellStop`, while exits are executed with market orders to keep the logic portable across data providers.
