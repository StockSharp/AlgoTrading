# Stop Hunter Strategy

## Overview
- Ports the MetaTrader 4 expert advisor **Stop Hunter** into the StockSharp high-level strategy framework.
- Focuses on round-number breakouts: the algorithm constantly searches for price levels whose rightmost `Zeroes` digits are zero and places stop orders just inside those thresholds.
- Keeps take-profit and stop-loss levels hidden from the broker by supervising exits internally, reproducing the "virtual" risk management used in the original EA.
- Implements the two-stage scaling logic of the source code: the first portion of a position is closed after the initial target, the remainder trails for double the distance.

## Data Flow and Subscriptions
1. Subscribes to **Level1** data (`SubscribeLevel1().Bind(ProcessLevel1)`) in `OnStarted`. Only the best bid/ask stream is required; candles or indicators are not used.
2. Every update stores the latest bid and ask and triggers the decision engine once the strategy is online and trading is allowed.
3. An optional chart area is created to visualize own trades when the strategy runs with charting enabled.

## Order Placement Logic
- **Round level detection**
  - Uses the instrument price step (`Security.PriceStep`) as the MQL `Point` analog.
  - Computes a round-step length: `roundStep = PriceStep * 10^Zeroes`.
  - Calculates the next round number above the bid (`Math.Ceiling(bid / roundStep) * roundStep`).
  - Adjusts the level when the ask is already inside the buffer, mirroring the original guard that avoids sending orders too close to the current spread.
  - Derives the lower round level (`LevelS`) one round-step below `LevelB` and performs the same safety adjustment against the bid.
- **Pending orders**
  - Places a **buy-stop** at `LevelB - DistancePoints * PriceStep` if no existing order is alive, long trading is enabled, and there is no short position open.
  - Places a **sell-stop** symmetrically at `LevelS + DistancePoints * PriceStep` if short trading is allowed and no long position exists.
  - Cancels stale pending orders whenever the computed round target moves forward or the price drifts away by more than one round-step plus `DistancePoints * 50`, matching the clean-up logic from the MQL version.
  - Keeps the total number of active slots (positions + pending orders) within `MaxLongPositions + MaxShortPositions`.

## Virtual Exit Management
- Tracks the average entry price and the current position volume.
- Uses two integer accumulators (`_takeProfitExtension`, `_stopLossExtension`) to reproduce the original hidden buffers:
  - First profit target: closes half of the position when the bid/ask reaches `TakeProfitPoints * PriceStep` in favor of the position.
  - After the first partial exit, extends both the profit and stop distances by another `TakeProfitPoints`/`StopLossPoints`, activating the "second trade" stage.
  - Final exit: closes the remaining volume either when the doubled target is reached or when the doubled stop-loss distance is hit.
- Closes at market using `BuyMarket` or `SellMarket`, mirroring the EA that issued market closes instead of broker-side stop-loss orders.
- Removes the opposite-side pending stop whenever a position is opened to avoid hedging, just like the original loop that deleted conflicting orders.

## Money Management
- Reimplements the `Call_MM()` function from the EA: `volume = balance / 100000 * RiskPercent`.
- Clamps the computed volume between `MinimumVolume` and `MaximumVolume` and rounds it to the instrument's volume step (or to 2/1/0 decimals depending on `MinimumVolume`).
- Partial exits reuse the current position size to calculate half-volume closes while respecting the volume step.

## Implementation Notes
- Uses only StockSharp high-level APIs (`BuyStop`, `SellStop`, `BuyMarket`, `SellMarket`, Level1 binding). No direct connector calls or indicator collections are required.
- Maintains internal state across resets with `ResetState()` and ensures tabs are used for indentation per repository guidelines.
- Guard clauses (`IsFormedAndOnlineAndAllowTrading`) prevent order submission before the strategy is fully initialized.
- `OnOwnTradeReceived` mirrors the MQL checks that confirmed successful closes before updating the `SecondTrade` flag.
- `OnOrderChanged` clears references to prevent stale handles when orders are canceled or rejected.

## Differences vs. the MQL Version
- Netting model: StockSharp strategies operate with a single net position. The default parameters still mimic the EA (one long and one short slot), but scaling into multiple simultaneous tickets is not supported beyond the net exposure.
- Risk computation uses `Portfolio.CurrentValue` (fallback to `BeginValue`) instead of `AccountFreeMargin`, providing a portable approximation in multi-asset environments.
- Virtual stop/take-profit distances reset cleanly when a new trade opens, avoiding the accumulation bug present in the historical EA code.
- All comments and documentation are written in English, while the README files additionally describe the strategy in Russian and Chinese as required by project guidelines.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `Zeroes` | 2 | Digits on the right-hand side that must be zero for a price to be considered a round level. |
| `DistancePoints` | 15 | Offset (in price points) between the round level and the stop entry. |
| `TakeProfitPoints` | 15 | Hidden take-profit distance in points. Also reused for the second-stage extension. |
| `StopLossPoints` | 15 | Hidden stop-loss distance in points (doubled after the first scale-out). |
| `EnableLongOrders` | true | Enables buy-stop placement. |
| `EnableShortOrders` | true | Enables sell-stop placement. |
| `RiskPercent` | 5 | Percentage of capital used to size the pending orders. |
| `MinimumVolume` | 0.1 | Minimum order size after rounding. |
| `MaximumVolume` | 30 | Cap for the calculated volume. |
| `MaxLongPositions` | 1 | Maximum number of long slots (position + pending). |
| `MaxShortPositions` | 1 | Maximum number of short slots (position + pending). |

## Usage Tips
1. Choose an instrument whose price step aligns with the MQL `Point` definition used by the original expert advisor. Forex pairs with fractional pips typically require `Zeroes = 2`.
2. Monitor broker tick size and volume step; adjusting `MinimumVolume` ensures the rounding logic matches exchange constraints.
3. Because exits are virtual, always keep the strategy online to avoid missing stop-loss conditions. Consider combining with StockSharp's `StartProtection()` if exchange-side risk management is required.
4. Review the Russian and Chinese README variants for localized explanations that traders can share with different teams.
