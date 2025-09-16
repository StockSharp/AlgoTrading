# Hedge Any Positions Strategy

## Overview
The **Hedge Any Positions Strategy** is a direct conversion of the original *Hedge any positions (barabashkakvn's edition)* MQL5 expert. The StockSharp version keeps the core idea intact: it monitors every open leg created by the strategy and, once a leg loses a defined number of pips, immediately opens an opposite position with an amplified lot size. The implementation relies on the high-level StockSharp API, so hedge orders are placed through market orders and position tracking is handled internally without custom order-routing code.

The strategy can optionally place an initial trade when it starts. Afterwards it simply reacts to adverse price moves and builds a ladder of hedging trades, marking each leg as hedged so the same position cannot trigger multiple opposite entries.

## Hedging Workflow
1. **Candle feed** – a configurable `CandleType` drives the strategy. Only finished candles are processed.
2. **Loss calculation** – on each candle close the strategy checks whether the close price moved against any open leg by at least `LosingPips` multiplied by the computed pip size.
3. **Hedge execution** – if a losing leg is found, a market order in the opposite direction is sent. The order volume equals the original leg volume multiplied by `LotCoefficient`, rounded to the instrument volume step and clipped to the allowed minimum/maximum volume.
4. **State update** – once an opposite order is dispatched, the original leg is flagged as hedged and the newly opened trade is stored as a fresh leg that can itself be hedged later if price reverses again.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `CandleType` | Timeframe used to evaluate price movements and trigger hedges. | 1-minute candles |
| `LosingPips` | Number of pips the price must move against a leg before a hedge is opened. | 5 |
| `LotCoefficient` | Multiplier applied to the original volume when submitting the hedge order. | 2.0 |
| `AutoPlaceInitialTrade` | When enabled the strategy sends the first trade automatically on start. | Disabled |
| `InitialVolume` | Order size used by the optional initial trade. Rounded to the instrument volume step. | 0.10 |
| `InitialDirection` | Side (buy or sell) used for the optional initial trade. | Buy |

> **Note:** Set the `Strategy.Volume` property to the base order size you want the strategy to use. The parameters above only control hedging-specific behaviour.

## Usage Guidelines
1. Assign a `Security`, `Portfolio`, and desired base `Volume` before starting the strategy.
2. Adjust `LosingPips` and `LotCoefficient` to reflect the volatility and risk tolerance for the selected instrument.
3. Enable `AutoPlaceInitialTrade` if you want the StockSharp version to create the very first position automatically; otherwise, manually open an initial leg or let another component do it.
4. Because the StockSharp high-level API works with net positions, the internal leg list is used to emulate the hedged structure. Monitor account exposure when running on netting accounts.
5. Review execution reports: every hedge is placed with a market order (`BuyMarket` or `SellMarket`).

## Differences from the Original Expert
- Margin validation, slippage checks, and verbose result logging were removed; StockSharp already reports execution problems through strategy events.
- The conversion uses finished candles instead of tick-by-tick data. Choose a sufficiently small timeframe if you need faster reaction times.
- Lot rounding now relies on `Security.VolumeStep`, `Security.MinVolume`, and `Security.MaxVolume` to stay compliant with the instrument's trading rules.
- Alerts, notifications, and the tester-only random initial trade from the MQL version were intentionally omitted. The optional automatic entry parameter replaces that behaviour.

## Recommended Enhancements
- Combine the hedging module with a separate entry strategy that defines when the first position should be created.
- Add equity-based shutdown rules or maximum-depth limits to prevent unbounded hedging chains.
- Integrate portfolio-level monitoring to ensure margin requirements remain within acceptable limits.
