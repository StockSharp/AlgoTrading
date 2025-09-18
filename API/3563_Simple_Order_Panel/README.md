# Simple Order Panel Strategy

Port of the MetaTrader 5 utility **SimpleOrderPanel**. The StockSharp version keeps the manual execution workflow but leverages the high-level strategy API for order routing, risk-based sizing, and protective order management.

## Key Features

- Manual buy/sell triggers sized either by fixed volume or account risk percentage.
- Configurable stop-loss and take-profit values that can be interpreted as absolute prices or MetaTrader-style points.
- One-click helpers for break-even, partial close, and full flattening of the current exposure.
- Pending entry emulation (limit or stop) that arms a trigger price and converts the request into a market order once reached.
- Protective logic that continuously monitors incoming Level1/trade data and closes the position when stops or targets are hit.

## Parameters

| Name | Description | Default |
| ---- | ----------- | ------- |
| `RiskCalculation` | `FixedVolume` uses the exact `RiskValue`. `BalancePercent` converts the percentage into a tradable lot size using stop distance and the instrument's step value. | `FixedVolume` |
| `RiskValue` | Lot size or balance percentage (depending on `RiskCalculation`). | `0.1` |
| `StopTakeCalculation` | `PriceLevels` treats the stop/take numbers as absolute prices. `PointOffsets` multiplies the values by the instrument point. | `PointOffsets` |
| `StopLossValue` | Stop-loss number (price or points). A zero disables the protective stop. | `300` |
| `TakeProfitValue` | Take-profit number (price or points). A zero disables the protective target. | `300` |
| `BuyMarketRequest` | Toggle that opens a long position using the calculated volume. | `false` |
| `SellMarketRequest` | Toggle that opens a short position using the calculated volume. | `false` |
| `BreakEvenRequest` | Moves the stop-loss to the recorded entry price. | `false` |
| `ModifyStopRequest` | Reapplies the stop-loss configuration to the current position. | `false` |
| `ModifyTakeRequest` | Reapplies the take-profit configuration to the current position. | `false` |
| `CloseAllRequest` | Closes the entire position regardless of direction. | `false` |
| `CloseBuyRequest` | Closes the long position only. | `false` |
| `CloseSellRequest` | Closes the short position only. | `false` |
| `PartialCloseRequest` | Closes `PartialVolume` lots from the current position. | `false` |
| `PartialVolume` | Volume used when executing a partial close. | `0.05` |
| `PlaceBuyPendingRequest` | Arms a pending buy (limit or stop, selected automatically). | `false` |
| `PlaceSellPendingRequest` | Arms a pending sell (limit or stop, selected automatically). | `false` |
| `PendingPrice` | Trigger price for the pending order helpers. | `0` |
| `CancelPendingRequest` | Removes all armed pending orders. | `false` |

All boolean toggles are meant to be set to `true` momentarily. The strategy resets them to `false` after the corresponding action is processed.

## Trading Logic

1. **Risk sizing** – when a trade request arrives, the strategy either uses the fixed volume or calculates the required lots so that `RiskValue%` of the portfolio is lost if the stop is hit. The computation uses the instrument `PriceStep` and `StepPrice` metadata.
2. **Market entries** – `BuyMarketRequest` and `SellMarketRequest` send market orders after validating the calculated volume. Existing positions in the same direction are ignored to avoid double entries.
3. **Pending entries** – arming a pending order stores the desired price, volume, and whether the entry behaves like a stop or a limit. Incoming Level1 updates trigger the request once the price condition is satisfied.
4. **Protective management** – the strategy records the effective entry price on each position increase and converts the stop/take configuration into target prices. Best bid/ask updates (or last trade when necessary) continuously check for stop-loss, take-profit, or break-even conditions and call `ClosePosition()` accordingly.
5. **Position utilities** – dedicated toggles close the entire exposure, only longs, only shorts, or a user-defined portion. Break-even, stop, and take buttons simply update the stored protective prices.

## Usage Notes

- Attach the strategy to an instrument that provides Level1 quotes; without bid/ask data pending orders cannot fire.
- To reproduce the MetaTrader experience, configure `StopTakeCalculation` and the numeric values exactly as in the panel.
- Pending orders are emulated; they will not appear on the exchange book until the trigger fires.
- The helper logs report every action, making it easier to monitor the manual workflow in the StockSharp logs panel.
