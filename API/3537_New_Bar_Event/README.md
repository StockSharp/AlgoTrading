# New Bar Event Strategy

## Overview
The **New Bar Event Strategy** is a high-level StockSharp sample that demonstrates how to detect the arrival of a fresh candle. It reproduces the behavior of the original MQL example that compared the open time of the current bar with the previously cached value to determine whether a new bar had started. The strategy does not execute any trading operations and focuses entirely on logging the detection process, making it a clear reference implementation for developers who need a reusable "new bar" trigger inside larger strategies.

## Strategy Logic
1. The strategy subscribes to candles using the timeframe selected in the `Candle Type` parameter.
2. Each finished candle triggers the `ProcessCandle` handler.
3. The handler compares the open time of the incoming candle with the stored value from the previous call.
4. If the stored value is empty, the strategy recognizes the event as the very first candle after the launch and logs a dedicated message (equivalent to the "first tick" branch in the MQL script).
5. If the open time differs from the cached value, the strategy logs that a regular new bar has started.
6. The cached open time is updated after every detection, ensuring the next candle is evaluated correctly.

Because only finished candles are processed, the handler is never called multiple times for the same bar. However, the code still keeps the guard clause that mirrors the defensive approach of the original source.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `Candle Type` | Candle timeframe (DataType) that controls which bars are monitored for new-bar events. | 1-minute timeframe |

The timeframe parameter is exposed through `StrategyParam<DataType>` so it can be optimized or edited from the UI, matching the conventions used across other converted strategies.

## Signals and Actions
- **First bar detected:** when the first finished candle arrives after the strategy starts, a detailed informational log is produced. This covers the scenario when the chart might already be mid-bar.
- **New bar detected:** every subsequent candle with a different open time triggers another informational log. The placeholder region after the log call is where trading actions or indicator updates could be inserted in a real-world system.

No orders are submitted automatically; the intention is to provide a building block that developers can extend with their own logic.

## Usage Notes
1. Attach the strategy to a security and choose the desired candle type.
2. Start the strategy and observe the log messages—each new bar will appear as a structured entry.
3. Integrate this code into larger strategies to replace ad-hoc timer checks with a reliable candle-driven event source.
4. Extend the `ProcessCandle` method with actual trading rules if you need reactions to the beginning of a bar (for example, evaluating indicator snapshots or resetting trailing variables).

## Limitations
- The strategy relies on candle data; it does not attempt to synthesize ticks or micro-bars.
- Since it demonstrates infrastructure logic, no performance optimizations or trade management features are included.
- Logging is handled through `LogInfo`, which is visible in the StockSharp strategy log window; additional persistence or notifications must be added separately.
