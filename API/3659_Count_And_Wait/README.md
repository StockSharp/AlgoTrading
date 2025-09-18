# Count And Wait Strategy

## Overview
The **Count And Wait Strategy** reproduces the behaviour of the original MetaTrader samples “Count and Pass” and “Pass Wait then Pass”. The strategy counts the number of tick messages received from the connected trading venue. Once the configurable counting threshold is reached the strategy logs the event, optionally enters a waiting phase for a second threshold, and finally resets the counters to start a new cycle. The logic can be used as a template for scenarios where a deterministic number of market updates must elapse before an action is taken.

## Parameters
- **Count Limit** – number of ticks that must arrive before the strategy reports a completed cycle. The default value is `50`, mirroring the original `count` input from the MetaTrader script.
- **Wait Limit** – number of ticks to wait after the action has been triggered before the counters reset. The default value is `0`. Setting it to zero disables the waiting phase and produces the behaviour of the “Count and Pass” script. Any positive value enables the waiting phase, matching the “Pass Wait then Pass” script via the `wait` input.

Both parameters are exposed as `StrategyParam<int>` instances so they can be optimised or adjusted from the user interface.

## Workflow
1. The strategy subscribes to trade ticks for the configured security when it starts.
2. Each incoming tick increments the counting counter until `Count Limit` is reached.
3. When the counter hits the limit the strategy writes an informational message (“Count limit X reached. Executing cycle action.”), clears the waiting counter, and either resets immediately (if `Wait Limit` is zero) or begins the waiting phase.
4. During the waiting phase the strategy increments the waiting counter on every tick until `Wait Limit` ticks were observed. Upon completion it logs “Wait limit X reached. Restarting counting phase.” and resets both counters.
5. The loop repeats for as long as the strategy is running.

No orders are created automatically; this implementation mirrors the structure of the MQL examples where the user injects custom logic into the “Your code goes here” blocks. You can replace the log calls with order placement or any other action that must be executed after deterministic tick counts.

## Conversion Notes
- Both MetaTrader examples relied on global counters updated inside `OnTick`. The StockSharp port uses local fields that are updated from the `SubscribeTrades().Bind(ProcessTrade)` callback, ensuring that each tick message is processed exactly once.
- Logging replaces the original `Alert` and `Comment` calls. It provides traceable feedback in the StockSharp logs without forcing trading actions.
- Parameters keep the same semantics as in the source scripts, allowing you to emulate either sample by changing only the `Wait Limit` value.
