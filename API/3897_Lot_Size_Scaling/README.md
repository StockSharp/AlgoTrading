# Lot Size Scaling Strategy

## Overview

The **Lot Size Scaling Strategy** is a direct conversion of the MetaTrader 4 script `LotSize.mq4`. The original code provided a massive lookup table that maps the current account balance to a recommended order volume. This StockSharp version keeps the behaviour intact while exposing the recommended volume through the `Strategy.Volume` property and the strategy comment field.

The strategy does not open orders on its own. Instead, it focuses on calculating and updating the appropriate volume value that can be consumed by other strategies or manual decisions inside StockSharp environments. The mapping covers balances from 20 up to 294 635 units and volumes from 0.01 up to 100 lots.

## Trading Logic

1. A balance/volume table is generated directly from the original MQL source. It contains 1 090 entries that exactly match the thresholds hardcoded in the EA.
2. On every start, profit-and-loss change, or personal trade update, the strategy:
   - Reads the current portfolio balance (falls back to the `MinimumBalance` parameter when the portfolio is not yet available).
   - Looks up the largest table entry that does not exceed the balance.
   - Assigns the mapped volume both to the `Volume` property and to the `RecommendedVolume` property for reference.
   - Publishes the information in the strategy comment so it can be monitored from the UI.
3. The strategy never registers orders automatically. Traders can use `Volume` for their own order placement rules or other automated modules.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `MinimumBalance` | Lower bound used before the platform reports an actual portfolio value. | `20` |
| `MinimumVolume` | Initial volume applied when the current balance is below the first table entry. | `0.01` |

Both parameters can be adjusted, but the lookup table always follows the original MQL mapping.

## Notes

- The table preserves every irregular step present in the source script (for example, alternating 294/295 currency units after 2.0 lots).
- Use this helper inside a larger strategy when a balance-dependent position sizing rule is required.
- The strategy invokes `StartProtection()` on start so that standard StockSharp protection logic is enabled for downstream consumers.
