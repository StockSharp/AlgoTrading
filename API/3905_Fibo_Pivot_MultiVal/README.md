# Fibo Pivot MultiVal Strategy

## Overview

The **Fibo Pivot MultiVal Strategy** is a StockSharp port of the MetaTrader 4 expert advisor `_Fibo_Pivot_multiVal.mq4`. The
strategy combines daily pivot points with Fibonacci retracement and extension ratios to deploy limit orders inside each price
zone that surrounds the pivot. Trading sessions, position targets, and halting rules follow the original expert advisor so that
risk control and execution behaviour remain familiar to traders who used the MetaTrader version.

## Core Logic

1. **Daily reference levels** are computed from the previous day's high, low, and close. Classic pivot levels (P, R1-R3, S1-S3)
   are accompanied by Fibonacci-based internal levels that split the distance between the pivot and the neighbouring support or
   resistance lines. Additional R3/S3 extensions project potential breakout targets.
2. **Intraday price action** is monitored on the configured candle timeframe (15 minutes by default). When the current close
   resides inside a particular pivot zone (for example between R2 and R3), the strategy activates the corresponding limit orders.
3. **Limit orders** are placed at the Fibonacci sub-levels. Each zone maintains both long and short orders, with the direction
   filtered by the `MidZoneOrderMode` parameter when the price oscillates between R1-R2 and S1-S2.
4. **Targets** adapt to market volatility. When `UseReversalTargets` is enabled, exits sit on the opposite side of the active
   Fibonacci band to capture mean-reversion bounces. When disabled, the algorithm compares the previous day's range with the
   `LimitPointOut` and `LimitPointIn` thresholds to decide whether to aim for extended breakouts (towards R3/S3 extensions) or
   deeper reversals (towards the pivot).
5. **Risk limits** pause new trades once the configurable daily or per-symbol profit/trade thresholds are exceeded. All pending
   orders are cancelled and trading resumes on the next session reset (before `StartTime`).
6. **Session management** mirrors the original EA: trading starts at `StartTime`, new entries stop after `FinishTime`, and all
   open exposure is flattened after `CloseAllTime`.

## Parameters

| Name | Default | Description |
| ---- | ------- | ----------- |
| `CandleType` | 15-minute candles | Timeframe used to build the decision candles. |
| `OrderVolume` | `0.1` | Volume for each limit order registered by the strategy. |
| `StartTime` | `00:01` | Session time of day that enables trading and resets counters. |
| `FinishTime` | `08:00` | Session time that disables new entries while keeping existing positions. |
| `CloseAllTime` | `12:00` | Session time that cancels orders and closes all positions. |
| `UseReversalTargets` | `true` | When true, targets stay inside the Fibonacci zone. When false, breakout/pivot targets are used based on the daily range. |
| `LimitPointIn` | `150` | Daily range threshold (points) that enforces pivot reversion targets when exceeded. |
| `LimitPointOut` | `50` | Daily range threshold (points) that encourages breakout targets when price action is compressed. |
| `LevelPf1` | `33` | Percentage used to split the Pivot–R1 and Pivot–S1 distance. |
| `LevelF1F2` | `50` | Percentage used to compute the intermediate level between R1–R2 and S1–S2. |
| `LevelF2F3` | `33` | Percentage used to compute the intermediate level between R2–R3 and S2–S3. |
| `LevelF3Out` | `40` | Percentage used to extend R3/S3 for breakout targets. |
| `MidZoneOrderMode` | `"bs"` | Allowed directions inside the mid zones (`"b"`=buy only, `"s"`=sell only, `"bs"`=both). |
| `DailyProfitTarget` | `50` | Daily profit limit in points. |
| `DailyTradeTarget` | `35` | Maximum number of completed trades per day. |
| `SymbolProfitTarget` | `150` | Per-symbol profit target in points. |
| `SymbolTradeTarget` | `15` | Maximum completed trades per symbol per day. |

## Order Management

* Each active zone keeps its own entry, take-profit, and optional stop orders. When an entry is filled, exit orders are
  recreated using the target/stop levels derived from the Fibonacci configuration.
* Filled exits update the daily and per-symbol statistics. Hitting any limit pauses trading until the next reset.
* Session boundaries automatically cancel entry orders. The `CloseAllTime` boundary additionally closes any open positions via
  market orders.

## Practical Tips

* The strategy expects instruments with well-defined price steps. Ensure the `Security` instance exposes `PriceStep` so that the
  point-to-price conversion matches the original EA.
* For assets with different volatility characteristics, adjust `LimitPointIn` and `LimitPointOut` so that breakout vs.
  mean-reversion behaviours trigger at appropriate ranges.
* If you prefer directional trades around the mid-zone (R1-R2 or S1-S2), set `MidZoneOrderMode` to `"b"` or `"s"` to allow only
  long or short setups.
* Use the built-in parameter optimisation support to backtest alternative Fibonacci ratios. All percentage parameters and
  thresholds expose `SetCanOptimize` in the source code, enabling automated scans inside StockSharp Designer.

## Differences from the Original Expert Advisor

* The StockSharp version works on a single security per strategy instance. To trade multiple symbols as in the MetaTrader EA,
  run separate strategy instances for each instrument.
* Position sizing is expressed directly in volume units rather than MetaTrader lots. Configure `OrderVolume` to match your
  broker's requirements.
* Order execution relies on the StockSharp high level API (`BuyLimit`, `SellLimit`, etc.). Broker-specific behaviour (such as
  pending order offsets) should be reviewed before deploying to production.

