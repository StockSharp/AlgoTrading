# Timed Buy Order Strategy

## Overview
The **Timed Buy Order Strategy** replicates the MetaTrader expert advisor `buy_order.mq4`, which submits a stream of market buy orders driven by a one-second timer. The StockSharp port keeps the same trading rhythm: it waits until the timer aligns with the expected second within the current minute and then pushes the next order. After a pre-defined number of fills the strategy stops itself automatically.

This implementation relies on the high-level StockSharp `Timer` service instead of manual loops. No market indicators or candle subscriptions are required, making the logic deterministic and timing-focused.

## Core Logic
1. When the strategy starts it activates risk protection via `StartProtection()` and starts a timer with the configured interval (default: one second).
2. Each timer callback checks whether the strategy is online and allowed to trade, and whether the current exchange second matches the expected sequence value.
3. If all checks succeed, the strategy sends a market buy order with the configured volume.
4. The process repeats until the target number of orders has been sent, after which the strategy stops.

The second-synchronization behaviour mirrors the original MQL expert: the first order is only dispatched when the seconds component reaches zero, and each following order is tied to the next second value.

## Parameters
| Name | Type | Default | Description |
| ---- | ---- | ------- | ----------- |
| `OrderVolume` | `decimal` | `0.01` | Quantity for each market buy order. A validation guard stops the strategy if the value is non-positive. |
| `OrdersToPlace` | `int` | `60` | Total number of sequential buy orders to submit before stopping. |
| `Interval` | `TimeSpan` | `1s` | Delay between timer callbacks. Keeping it at one second best reproduces the MQL timing, but other values are possible for experimentation. |

All parameters are exposed through StockSharp `StrategyParam<T>` objects, so they can be optimised or configured from UI tooling.

## Execution Flow
- **Initialisation** – resetting counters in `OnReseted()` ensures clean state when restarting or re-optimising.
- **Start** – in `OnStarted()` the timer begins and counters reset; protection is enabled once per lifecycle.
- **Timer Tick** – method `OnTimer()` performs the sequencing checks, logs the outgoing order, and stops the strategy when the final order is sent.
- **Completion** – helper `CompleteStrategy()` prevents duplicate shutdown attempts and calls `Stop()` exactly once.

## Conversion Notes
- The MQL function `EventSetTimer(1)` is mapped to `Timer.Start(TimeSpan.FromSeconds(1), OnTimer)`.
- Order comments and magic numbers used in MetaTrader do not have direct equivalents in StockSharp, so logging is used instead to trace progress.
- The strategy keeps the “60 orders per minute” concept by matching the seconds component rather than counting timer triggers.

## Usage Tips
1. Assign the desired security and portfolio before starting the strategy.
2. Adjust `OrderVolume` to match the instrument’s lot size and broker rules.
3. If you need fewer orders, reduce `OrdersToPlace`; to disable the second-based pacing entirely, set `Interval` to any value and remove the second matching in the code (advanced modification).
4. Monitor the log output to track order submissions and ensure that the timer alignment behaves as expected.

## Limitations
- The strategy only buys; there is no exit logic other than manual intervention or protective stops managed by the broker.
- Order placement is limited by the accuracy of the timer service provided by the connection and operating system; large delays could desynchronise the sequence.

## Files
- `CS/TimedBuyOrderStrategy.cs` – main C# implementation.
- `README_cn.md` – Chinese documentation.
- `README_ru.md` – Russian documentation.

A Python port is intentionally omitted per project instructions; create it later if required.
