# Master Exit Plan Strategy

## Overview

`MasterExitPlanStrategy` reproduces the risk-management logic of the MetaTrader expert advisor "Master Exit Plan" using StockSharp's high-level API. The strategy does not open new trades. Instead it supervises existing exposure, applies a combination of hidden and visible stop rules, trails pending orders and closes everything once equity reaches a configured profit target.

The implementation subscribes to one-minute candles to emulate the `iOpen(symbol, PERIOD_M1, 1)` calls from the original script. All timers are driven by the strategy scheduler and evaluated every second, matching the behaviour of the MetaTrader `EventSetTimer(1)` loop.

## Features

- **Equity target exit** – closes all positions when portfolio equity gains reach the configured percentage.
- **Static and dynamic stop levels** – monitors both stop distances from the entry price and minute-based dynamic anchors.
- **Hidden stop handling** – executes protective exits internally instead of relying on exchange orders.
- **Trailing stop module** – activates after a minimum money gain and trails the stop with spread compensation.
- **Pending order trailing** – automatically re-registers buy-stop and sell-stop orders so they follow the market.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `EnableTargetEquity` | Enable equity target liquidation. | `false` |
| `TargetEquityPercent` | Percentage of current balance used as the target. | `1` |
| `EnableStopLoss` | Activate static broker-style stop-loss. | `false` |
| `StopLossPoints` | Static stop distance (MetaTrader points). | `2000` |
| `EnableDynamicStopLoss` | Tie the hard stop to the previous minute open. | `false` |
| `DynamicStopLossPoints` | Dynamic stop distance (points). | `2000` |
| `EnableHiddenStopLoss` | Enable hidden static stop-loss. | `false` |
| `HiddenStopLossPoints` | Hidden static stop distance (points). | `800` |
| `EnableHiddenDynamicStopLoss` | Enable hidden dynamic stop based on the minute open. | `false` |
| `HiddenDynamicStopLossPoints` | Hidden dynamic stop distance (points). | `800` |
| `EnableTrailingStop` | Enable the trailing stop module. | `false` |
| `TrailingStopPoints` | Trailing distance maintained behind price (points). | `5` |
| `TrailingTargetPercent` | Minimum profit in % of balance before trailing activates. | `0.2` |
| `SureProfitPoints` | Extra points that must be secured before arming the trailing stop. | `30` |
| `EnableTrailPendingOrders` | Enable trailing of active stop orders (entries). | `false` |
| `TrailPendingOrderPoints` | Offset in points for trailing pending stop orders. | `10` |

## Usage Notes

1. Attach the strategy to a security that is already managed by another entry module or manual orders. Set `Volume` according to the contracts you need to close when flattening.
2. Provide a portfolio that reports `Portfolio.CurrentValue`. The strategy uses this value to approximate `AccountBalance` and `AccountEquity` from MetaTrader. If the value is missing, the equity target logic stays idle.
3. The strategy evaluates best bid/ask quotes when checking stop conditions. Ensure level1 data is available so spread-aware calculations are meaningful.
4. Hidden stops and trailing exits are implemented as software-managed market orders. Broker-side stop orders are **not** created; the behaviour mirrors the "hidden" nature of the original EA.

## Differences from the MQL Version

- Stop levels are enforced by issuing market orders when thresholds are breached. The original EA modified the `OrderStopLoss` field; StockSharp uses active monitoring instead.
- Dynamic stop calculations rely on the last completed one-minute candle delivered through `SubscribeCandles`. If this subscription is missing, dynamic rules stay disabled.
- Pending order trailing ignores protective stop orders created by other strategies because `MasterExitPlanStrategy` itself does not register them.
- Equity checks use `Portfolio.CurrentValue` (fallback to `Portfolio.BeginValue`) instead of `AccountBalance`/`AccountEquity`.

## Testing

The strategy contains no automated tests. Use StockSharp's tester with historical data to verify behaviour on your instruments before deploying to production.
