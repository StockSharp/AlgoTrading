# Bitex One Market Maker Strategy

## Overview
The **Bitex One Market Maker Strategy** reproduces the asynchronous quoting robot from the original `BITEX.ONE MarketMaker.mq5` source. The algorithm continuously places pairs of limit orders around a reference price and maintains an equal number of levels on the bid and ask sides. The strategy was rewritten for StockSharp using the high-level API: quote management is driven by order book and level 1 subscriptions, while risk and volume normalisation rely on instrument metadata (`PriceStep`, `VolumeStep`, and `MinVolume`).

## Trading Logic
1. Determine the *lead price* from the selected `PriceSource`. By default the strategy expects mark prices, but it can use the main order book or an auxiliary instrument (index or mark symbol) via the `LeadSecurity` parameter.
2. Compute the distance between price levels as `ShiftCoefficient * lead price` and create a symmetric ladder of quotes above and below the reference.
3. Clamp the total exposure on each side to `MaxVolumePerLevel * LevelCount`. Executed trades immediately reduce available volume so the grid always reflects the current position.
4. Normalise prices and volumes using the security tick size and volume step. The algorithm cancels outdated orders and registers new ones whenever price or volume drift beyond the tolerance inherited from the original MQL code (0.05% price threshold and half-step volume threshold).
5. All active orders are cancelled during stop/reset events to keep the book clean.

## Parameters
- `MaxVolumePerLevel` – maximum volume quoted at any single price level. Affects both sides of the book and acts as a cap when the current position grows.
- `ShiftCoefficient` – relative offset from the lead price applied for each incremental level (`leadPrice ± shift * levelIndex`).
- `LevelCount` – number of quoting levels per side. Each level creates one buy and one sell limit order.
- `PriceSource` – enumerated value (`OrderBook`, `MarkPrice`, `IndexPrice`) defining where the reference price originates.
- `LeadSecurity` – optional security used when external mark or index prices are required. If omitted, the main strategy security provides the reference.

## Conversion Notes
- The asynchronous order management from MetaTrader (SendAsync/ModifyAsync/RemoveOrderAsync) is mapped to StockSharp’s `BuyLimit`/`SellLimit` helpers combined with explicit cancellation when tolerances are exceeded.
- The position balancing logic (`max_pos * level_count ± position`) is preserved to keep the ladder centred and risk aware.
- The lead price selection mimics the suffix logic of the original robot (`symbol`, `symbolm`, `symboli`) by allowing a custom `LeadSecurity` combined with a `PriceSource` hint.
- Timer-driven checks in MQL are replaced with reactive updates triggered by order book/level 1 messages and portfolio events.

## Usage Notes
- Ensure that the connected adapter provides market depth or level 1 data for both the trading symbol and the optional `LeadSecurity`.
- When using mark or index feeds, subscribe to the corresponding instruments before starting the strategy so that the lead price becomes available immediately.
- Consider enabling portfolio protection or additional risk management in the hosting environment if the exchange requires stringent quote-to-trade ratios.
- The strategy does not start quoting until a positive lead price is received; verify connectivity if no orders appear after start-up.
