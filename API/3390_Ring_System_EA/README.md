# Ring System EA Strategy

This strategy ports the multi-currency "RingSystemEA" grid hedging expert from MetaTrader 4 to the StockSharp high level API. It arranges a configurable list of currencies into triangular rings (three currencies generate three correlated pairs) and manages two hedged baskets per ring: a **plus** basket (long/short/long) and a **minus** basket (short/long/short). The strategy continuously monitors floating profit across every ring, applies step-based martingale-style reinforcement when losses exceed configured thresholds, and coordinates global or per-side exits when profit or loss targets are reached.

## Trading Logic

* Build all unique combinations of three currencies from the ordered `CurrenciesTrade` list (e.g. EUR/GBP/AUD produces EURGBP, EURAUD, and GBPAUD).
* Each ring maintains two synchronized baskets:
  * **Plus basket** opens BUY on the first pair, SELL on the second pair, BUY on the third pair.
  * **Minus basket** opens the mirrored SELL/BUY/SELL sequence.
* Baskets are opened automatically once the ring has price data and the session filter allows trading. Both sides may run simultaneously, or only one side depending on `SideOpenOrders`.
* When an active basket draws down beyond the `StepOpenNextOrders` threshold (optionally scaled geometrically or exponentially), a new layer of orders is added using volume progression rules (`LotOrdersProgress`).
* Baskets are closed when their floating PnL satisfies the chosen exit mode:
  * `SingleTicket` closes the plus and minus baskets independently.
  * `BasketTicket` closes both baskets together once their combined profit hits the target.
  * `PairByPair` closes individual pairs when their PnL exceeds the target.
* Protective exits mirror the MT4 logic. Depending on `TypeCloseInLoss` the strategy either closes entire baskets, halves the exposure, or lets the baskets recover without forced exits.
* Optional session guard replicates the wait-after-Monday-open and stop-before-Friday-close behaviour.
* Parameters closely match the original EA. Auto lot sizing uses the current portfolio value and `RiskFactor`, while the "fair lot" option compensates for tick value differences inside a ring.

## Key Parameters

| Parameter | Description |
| --- | --- |
| `CurrenciesTrade` | Ordered currency list that defines how rings are generated. |
| `NoOfGroupToSkip` | Comma-separated ring numbers to ignore. |
| `SideOpenOrders` | Choose plus side, minus side, or both. |
| `OpenOrdersInLoss` + `StepOpenNextOrders` | Controls when additional orders are added while baskets are losing. |
| `StepOrdersProgress` | Multiplier applied to the loss threshold for each additional layer. |
| `LotOrdersProgress` | Scaling rule for volumes of subsequent orders. |
| `TypeCloseInProfit` / `TargetCloseProfit` | Profit taking logic and thresholds. |
| `TypeCloseInLoss` / `TargetCloseLoss` | Protective exits in loss. |
| `AutoLotSize`, `RiskFactor`, `ManualLotSize`, `UseFairLotSize` | Money management options. |
| `ControlSession`, `WaitAfterOpen`, `StopBeforeClose` | Weekly trading window guard. |
| `MaxSpread`, `MaximumOrders`, `MaxSlippage` | Risk constraints. |

## Behavioural Notes

* The StockSharp port keeps state in managed structures rather than raw arrays, but the trade flow mirrors the MT4 expert: open balanced baskets, monitor basket PnL, reinforce at drawdown steps, and close on profit or risk events.
* All indicators are implicit; the strategy relies solely on price subscriptions and account PnL to make decisions.
* Orders are tagged with `StringOrdersEA` so that external post-processing tools can identify them.
* Market orders use the strategy portfolio; connect the desired instruments before starting.

## Differences From the Original EA

* Spread filtering is simplified: the StockSharp port validates the configured `MaxSpread` through candle data rather than tick snapshots.
* Auto-step mode reuses the manual step value because MetaTrader-specific margin calculations are not available inside StockSharp.
* The UI drawing and file logging features of the MT4 version are omitted. `SaveInformations` now writes detailed diagnostics to the log instead of the chart.
* Position sizing uses current portfolio value; adjust `RiskFactor` to calibrate volume.

## Usage Tips

1. Connect and map all currency pairs referenced by `CurrenciesTrade`. Prefix/suffix helpers support broker-specific symbols.
2. Set `SideOpenOrders` to control whether the strategy should maintain both baskets or operate in a single direction.
3. Tune `StepOpenNextOrders`, `StepOrdersProgress`, and `LotOrdersProgress` carefully; these parameters shape the martingale progression and risk exposure.
4. Review the log messages when `SaveInformations` is enabled to understand how rings evolve and when baskets are added or closed.

This port preserves the core hedged grid behaviour of the MT4 expert while adapting it to StockSharp's event-driven architecture and parameter system.
