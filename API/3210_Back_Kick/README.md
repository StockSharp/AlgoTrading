# Back Kick Strategy

The **Back Kick** strategy is a hedged breakout system converted from the MetaTrader 5 expert advisor `Back kick.mq5`. It continuously maintains a two-sided exposure by opening both a long and a short market position at the close of every bar. Each leg is protected with symmetrical stop-loss and take-profit distances expressed in pips. The StockSharp port keeps the paired positions independent by tracking their state manually instead of relying on the aggregated net position.

## Trading Logic

1. Subscribe to the configured time-frame candles. When a candle closes and there are no active hedged legs, request a new pair of entries.
2. Immediately submit a market buy and a market sell order using the same volume. Each leg keeps its own stop-loss and take-profit offsets converted from pip distances.
3. Monitor best bid/ask prices from Level 1 data. If a leg reaches its protective price it is closed with a market order, while the opposite leg remains active until its own exit is triggered.
4. After both legs are flat, the strategy waits for the next completed candle before recreating the hedge.

This behaviour mirrors the original expert, which constantly re-enters in both directions to capture abrupt price "kicks".

## Parameters

| Name | Description | Default | Notes |
| ---- | ----------- | ------- | ----- |
| `OrderVolume` | Volume used for each hedge leg. | `0.1` | Normalised to the instrument `VolumeStep`, must respect `MinVolume`/`MaxVolume`. |
| `StopLossPips` | Stop-loss distance in pips. | `50` | Set to `0` to disable the protective stop for both legs. |
| `TakeProfitPips` | Take-profit distance in pips. | `140` | Set to `0` to disable the protective take-profit. |
| `CandleType` | Time-frame that triggers new hedged pairs. | `15m` | Accepts any `TimeFrame` supported by the selected security. |
| `LogDiagnostics` | Enables verbose logging about entries and exits. | `false` | Useful for debugging fill sequences. |

## Implementation Notes

- **Pip conversion** – The original EA adjusts pip size for 3/5 digit symbols. The StockSharp port replicates this by multiplying the price step with `10` when necessary.
- **Manual hedging model** – StockSharp uses net positions, so the strategy keeps per-leg state (`PositionState`) and dispatches explicit market orders for exits. This allows the behaviour to resemble the MT5 hedged account mode.
- **Risk management** – Stop-loss and take-profit levels are optional. If either is disabled, that leg will only close when the opposite protective level is hit or via external management.
- **Protection service** – `StartProtection()` is still invoked to let the framework monitor unexpected disconnects even though custom exit logic is implemented.

## Usage

1. Attach the strategy to a security with reliable Level 1 data (bid/ask) and the desired time-frame candles.
2. Configure the pip distances and trade volume according to your risk profile.
3. Start the strategy; it will wait for the next candle close before submitting the hedged pair.
4. Monitor the logs or chart to observe how each leg exits independently.

## Differences from the MT5 Version

- Money-management based on risk percentage is not transferred; use `OrderVolume` to control trade size.
- Because StockSharp aggregates portfolio positions, the strategy emulates hedging through internal bookkeeping. This ensures behaviour close to the original expert while remaining compatible with brokers that net positions.
- Broker-specific freeze/stop level checks are omitted. Instead, the volume normalisation routine throws descriptive exceptions if exchange limits are violated.

## Files

- `CS/BackKickStrategy.cs` – Strategy implementation using the high-level StockSharp API.
- `README.md` – English documentation (this file).
- `README_ru.md` – Russian documentation.
- `README_cn.md` – Chinese documentation.
