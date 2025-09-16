# Tunnel Gen4 Hedged Grid Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy replicates the logic of the MetaTrader "Tunnel gen4" expert advisor using the StockSharp high-level API. It maintains a market neutral hedge by opening an initial buy/sell pair, doubles the position in the direction of the break-out once the price travels a configurable number of pips, and exits the entire basket when the same distance is covered again beyond the second anchor.

## Trading Logic

- **Initial hedge:** As soon as no exposure exists, the strategy sends simultaneous market buy and sell orders with volume `StartVolume`. The first fill defines the reference price for all subsequent decisions.
- **Step detection:** The configured `StepPips` is converted to a price offset using the instrument tick size (with automatic adjustments for three- and five-decimal forex quotes). Best bid/ask updates from the Level 1 stream are compared against this offset.
- **Reinforcement order:** When the best bid moves up by at least one step from the first fill, a sell order with twice the base volume is sent. When the best ask moves down by at least one step, a buy order of the same size is issued instead. The first fill of this order becomes the second anchor.
- **Cycle termination:** After the second anchor is active, any further step-sized move in either direction triggers a full liquidation of all open positions. Once both sides are closed the state resets and a new cycle can start.
- **Volume validation:** Strategy start-up checks that both the initial and doubled volumes respect the instrument's minimum, maximum, and increment requirements so that every order sent to the connector is executable.

## Entry Conditions

### Long reinforcement
- There is at least one open position from the initial hedge.
- The second anchor has not been created yet.
- Current best ask price is less than or equal to `first_fill_price - StepPips_in_price`.

### Short reinforcement
- There is at least one open position from the initial hedge.
- The second anchor has not been created yet.
- Current best bid price is greater than or equal to `first_fill_price + StepPips_in_price`.

## Exit Management

- **Basket closure:** Once the second anchor is defined, if the best bid rises above `second_anchor + StepOffset` or the best ask falls below `second_anchor - StepOffset`, market orders are submitted to close the cumulative long and short exposure. Closing orders are tracked to ensure the state resets only after all trades are confirmed.
- **State reset:** After both sides are closed and no closing orders remain active, the strategy clears internal anchors and waits for a new hedge to be opened.

## Data and Indicators

- Level 1 subscription delivers best bid and best ask prices used for step comparisons.
- No additional indicators are required; the whole logic works on raw quote updates.
- Price step conversion mimics the MetaTrader point-to-pip adjustment so forex symbols with three or five decimals behave the same as in the source expert.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `StartVolume` | Volume of the buy and sell orders that form the initial hedge. |
| `StepPips` | Distance in pips that triggers the reinforcement order and the subsequent basket exit. |

## Implementation Notes

- StockSharp maintains a net position per security. The strategy keeps internal exposure counters to emulate the separate long and short tickets used by the MetaTrader expert and issues market orders with the accumulated volumes when closing the basket.
- Because the logic relies on real-time spreads, provide Level 1 data in both backtests and live trading sessions. Missing bid/ask information disables the trading loop.
- Ensure the trading account supports simultaneous buy and sell orders for the same instrument, as the algorithm assumes both sides of the hedge can coexist until the exit condition is met.
