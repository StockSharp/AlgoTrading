# MartingaleEA-5 Levels Strategy (StockSharp)

The **MartingaleEA-5 Levels Strategy** is a direct port of the MetaTrader 5 expert advisor "MartingaleEA-5 Levels" to the StockSharp high-level API. The system supervises an existing position and builds a five-step averaging grid whenever the market moves against it. All logic runs on finished candles, which keeps the behaviour reproducible in both historical tests and live trading.

## Trading Logic

1. **Monitoring existing exposure** – the strategy expects an initial long or short position to be present. You can open the first trade manually or through any other strategy.
2. **Adverse-move detection** – on each completed candle the strategy measures how far the current price has travelled away from the worst-priced entry of the active group (highest long or lowest short).
3. **Martingale additions** – if the floating loss on the group is negative and the adverse move exceeds the configured cumulative distances, the strategy sends extra market orders. Each additional order multiplies the previous one by `VolumeMultiplier`. Up to five levels can be configured; the `MaxAdditions` parameter limits how many of them are actually used.
4. **Profit and loss targeting** – while a group is open the strategy continuously sums the unrealised PnL for that direction. Once the total reaches `TakeProfitCurrency` or drops below `StopLossCurrency`, all orders on that side are closed with a market order and the martingale counters are reset.
5. **Volume normalisation** – every order volume goes through the instrument's `VolumeStep`, `MinVolume`, and `MaxVolume` to avoid sending non-executable quantities.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `EnableMartingale` | Turns the averaging and liquidation logic on or off. | `true` |
| `VolumeMultiplier` | Factor applied to the previous order volume when adding a new level. | `2.0` |
| `MaxAdditions` | Maximum number of martingale steps per direction (up to five). | `4` |
| `Level1DistancePips` | Initial adverse distance (in pips) before opening the second order. | `300` |
| `Level2DistancePips` | Additional distance required for the third order. | `400` |
| `Level3DistancePips` | Additional distance required for the fourth order. | `500` |
| `Level4DistancePips` | Additional distance required for the fifth order. | `600` |
| `Level5DistancePips` | Additional distance required for the sixth order (if allowed). | `700` |
| `TakeProfitCurrency` | Unrealised profit (account currency) that closes the whole group. | `200` |
| `StopLossCurrency` | Unrealised loss (account currency) that forces an emergency exit. | `-500` |
| `CandleType` | Timeframe used for evaluations (default 1-minute candles). | `TimeFrame(1m)` |

> **Pip conversion** – every distance is multiplied by the instrument price step (`PriceStep` or `MinPriceStep`). For symbols quoted in fractional pips adjust the values accordingly.

## Notes and Recommendations

- The implementation mirrors the original EA, including its assumption that only one directional basket is active at a time. Opening positions simultaneously in both directions will cause each side to be managed independently.
- Because the strategy reacts only on candle closes, choose a timeframe that matches the desired responsiveness. Lower timeframes emulate tick-level behaviour more closely.
- Martingale techniques amplify risk. Always backtest with realistic slippage and commission models and define conservative stop levels before enabling the strategy on live markets.
- The strategy does not create a Python port yet. Only the C# high-level implementation is included as requested.
