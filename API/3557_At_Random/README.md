# At Random Strategy

## Overview
This strategy is a StockSharp port of the MetaTrader 5 expert advisor "At random" (MQL ID 39835). The original bot demonstrates how a purely random decision process behaves when it is forced to be always in the market. Every completed bar triggers a coin flip that determines whether the next action is to buy or to sell. The StockSharp version keeps the same idea but expresses it with high-level API primitives (`SubscribeCandles`, `BuyMarket`, `SellMarket`) and integrates smoothly with Designer or Runner.

The implementation intentionally avoids take-profit, stop-loss or trailing stops, mirroring the reference MQL script. It therefore serves as a testing harness or a pedagogical example rather than a profitable strategy.

## Trading logic
1. Subscribe to the configured candle series (`CandleType`). The default interval is 15 minutes to mimic the MetaTrader "current timeframe" behaviour.
2. As soon as a candle is finished, check whether a previous trade must be closed. When `CloseBeforeReversal` is enabled the strategy flattens the position and waits for confirmation that no exposure remains before issuing the next order.
3. Generate a random direction using a pseudo random number generator. The optional `RandomSeed` parameter allows deterministic sequences for reproducible backtests.
4. Submit a market order using the fixed `TradeVolume`. Long and short trades are symmetric and there are no protective orders. Logging can be enabled via `LogSignals` to trace each random decision.

Because each candle triggers only one random decision, the strategy is either flat or carries a single position at any time. Positions are only reversed or closed when the next bar appears.

## Order management and risk
- All entries and exits are performed with `BuyMarket` / `SellMarket` using the configured volume. There are no limit or stop orders.
- If `CloseBeforeReversal` is disabled, the strategy may hold positions back-to-back: a new random signal can immediately open the opposite side without explicitly closing the previous trade first.
- No money management or account protection is implemented. The purpose of the port is to reproduce the reference expert advisor's behaviour for educational and infrastructure testing scenarios.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `TradeVolume` | Base order size used for every random entry. Must remain positive. |
| `CloseBeforeReversal` | Forces the strategy to close the current position before taking the next random trade. |
| `LogSignals` | Writes `AddInfoLog` messages whenever a random direction is generated. |
| `CandleType` | Timeframe that produces the candle series driving the random coin flip. |
| `RandomSeed` | Seed value for the pseudo random number generator. Use `0` to rely on the system clock. |

## Usage notes
- The port keeps the absence of take-profit and stop-loss levels just like the MQL reference. Any risk control must be added manually if the strategy is used for experiments with real capital.
- Deterministic seeds are useful to create reproducible datasets when optimising or benchmarking random behaviour.
- Enabling logging is recommended during tests because a pure random strategy offers little visual feedback on the chart.

