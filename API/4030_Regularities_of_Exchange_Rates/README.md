# Regularities of Exchange Rates Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This StockSharp strategy is a faithful C# conversion of the MetaTrader 4 expert advisor **Strategy_of_Regularities_of_Exchange_Rates.mq4**. The system was designed as a daily breakout straddle: it brackets the market with stop orders when a specific hour arrives and keeps those orders active until the nightly closing hour. Any filled position is supervised by both a broker-side stop-loss and an intraday take-profit watchdog so that trades do not linger beyond the defined trading session.

Unlike indicator-driven systems, the logic focuses solely on time and distance. When the schedule says the market should be ready, the strategy measures a fixed offset in broker points (pips) from the current bid and ask and places a pair of symmetrical stop orders. The code automatically adapts the point calculation to symbols with 3- or 5-digit quotes, matching the behaviour of the original MQL version.

## Trading Logic

1. **Opening hour** – once a finished candle reports `OpeningHour`, the strategy cancels any leftover pending orders and submits a *buy stop* above the current ask and a *sell stop* below the current bid. The distance is `EntryOffsetPoints * point`, where the `point` value is derived from the instrument `PriceStep` and adjusted for fractional quotes.
2. **Protective orders** – immediately after start-up the strategy enables `StartProtection` with the configured `StopLossPoints`. Any executed trade therefore receives a broker-side stop-loss identical to the original EA.
3. **Take profit supervision** – on every completed candle the algorithm checks whether the current profit exceeds `TakeProfitPoints * point`. If so, it closes the position at market. This mirrors the original `OrderClose` loop that exited when profit reached the threshold.
4. **Closing hour** – when the clock reaches `ClosingHour`, the strategy forcefully closes any open positions and cancels the stop orders, ensuring the book is flat for the next session.
5. **Daily reset** – a new batch of pending orders is sent only once per trading day, preventing duplicates while still respecting the original intent of a single setup per session.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `OpeningHour` | `9` | Hour (0–23) when the pair of stop orders is placed. |
| `ClosingHour` | `2` | Hour (0–23) when pending orders are removed and any open trades are flattened. |
| `EntryOffsetPoints` | `20` | Distance in broker points from the current bid/ask to the stop orders. |
| `TakeProfitPoints` | `20` | Profit target in broker points that triggers a market exit. Set to `0` to disable the manual take profit. |
| `StopLossPoints` | `500` | Distance in broker points for the protective stop attached via `StartProtection`. |
| `OrderVolume` | `0.1` | Volume of each stop order. |
| `CandleType` | `30 minute time frame` | Candle series used to evaluate the schedule. Any timeframe ≤ 1 hour keeps the behaviour consistent with the MQL script. |

## Conversion Notes

- The original expert advisor worked on tick events and referenced `Hour()` directly. In StockSharp the strategy listens to finished candles and uses their opening hour, which preserves the once-per-hour logic while staying within the repository guidelines about candle states.
- Pending orders are normalised with `Security.ShrinkPrice` so that the generated prices always match the instrument tick size.
- Stop management delegates to `StartProtection`, recreating the platform-generated stop-loss that MetaTrader attached during `OrderSend`.
- The code tracks the last trading date to avoid resubmitting the same bracket multiple times within the same day, something that could happen on sub-hour timeframes in the original EA.
- Extensive inline comments clarify each step of the workflow for future maintenance or experimentation.
