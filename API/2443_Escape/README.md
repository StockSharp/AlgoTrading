# Escape Strategy

This strategy trades based on simple moving averages of candle open prices. It compares the most recent 5-minute close with two moving averages calculated on the open price:

- **Fast SMA (4 periods)** – used as threshold for short entries.
- **Slow SMA (5 periods)** – used as threshold for long entries.

## How it works

1. On each finished 5-minute candle the strategy updates two SMAs of the candle open price.
2. If there is no active position:
   - Enter **long** when the latest close is below the slow SMA.
   - Enter **short** when the latest close is above the fast SMA.
3. After entering a position the strategy sets fixed stop-loss and take-profit levels measured in price units.
4. Position is closed when either take-profit or stop-loss is reached.

The logic uses high level StockSharp API and is intended for educational purposes.

## Parameters

| Name | Description | Default |
|------|-------------|---------|
| `FastLength` | Period of the fast SMA. | `4` |
| `SlowLength` | Period of the slow SMA. | `5` |
| `TakeProfitLong` | Take profit distance for long trades in price units. | `25` |
| `TakeProfitShort` | Take profit distance for short trades in price units. | `26` |
| `StopLossLong` | Stop loss distance for long trades in price units. | `25` |
| `StopLossShort` | Stop loss distance for short trades in price units. | `3` |
| `CandleType` | Candle type used for analysis. | `TimeFrame(5m)` |

All parameters can be optimized via the StockSharp optimizer.
