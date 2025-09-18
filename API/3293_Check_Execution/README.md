# Check Execution Strategy

## Overview
The Check Execution strategy reproduces the behaviour of the original MQL expert that repeatedly modifies a broker order to measure execution quality. The algorithm can test either a pending buy stop or a protective sell stop that guards a long position opened with a market order. Every modification records both the observed spread and the time required for the trading venue to accept the change, making it straightforward to evaluate latency-sensitive conditions offered by a broker.

## Core Logic
1. Subscribe to best bid/ask updates through the high-level `SubscribeLevel1` API.
2. Place the initial test order depending on the selected mode:
   - **Pending** – submit a buy stop above the current ask price.
   - **Market** – buy at market, then submit a protective sell stop below the latest ask.
3. On every quote update:
   - Update the rolling average of the bid/ask spread using a `SimpleMovingAverage`.
   - Re-register the tracked order at the new offset from the ask price when a change is required and a previous request is not awaiting confirmation.
   - Measure the execution latency as soon as the order returns to the `Active` state and feed it into a second `SimpleMovingAverage` to obtain the running average delay in milliseconds.
4. Repeat the modification cycle until the configured number of iterations is reached. Afterwards the strategy cancels any remaining pending/stop orders, closes the opened long position if needed, and prints the aggregated spread and latency statistics.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `Volume` | Trading volume used for every order. | `0.01` |
| `Iterations` | Number of modify attempts for averaging. Limited to 1–500. | `30` |
| `Order Mode` | Selects the workflow: `Pending` or `Market`. | `Pending` |
| `Pending Offset` | Distance in price steps above the ask for the test buy stop. | `100` |
| `Stop Offset` | Distance in price steps below the ask for the protective sell stop. | `100` |

## Behaviour Notes
- Volume values are normalised to the security `VolumeStep`, `MinVolume`, and `MaxVolume` constraints to avoid rejected orders.
- Price offsets are translated into actual prices using the instrument `PriceStep`. A default step of `0.0001` is used if the security does not provide one.
- The strategy only counts a modification when the trading venue acknowledges the request by moving the order into the `Active` or `Done` state. Each confirmation updates both the execution timer and the modification counter.
- Once the target number of iterations is reached the strategy automatically stops modifying orders, cancels pending protection, closes any test position, and logs a summary message with the measured averages.

## Differences from the MQL Version
- The spread and execution averages are computed with StockSharp `SimpleMovingAverage` indicators instead of manual arrays.
- Order management uses high-level helpers such as `BuyMarket`, `BuyStop`, `SellStop`, and `ReRegisterOrder` to stay consistent with the StockSharp strategy framework.
- The user interface feedback is provided through the strategy log rather than on-chart comments and graphical objects.
