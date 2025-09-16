# HTH Basket Strategy

This folder contains the StockSharp high-level conversion of the MetaTrader expert **HTH.mq4** ("Hedge That Hedge"). The original EA opens a four-leg basket of major FX pairs shortly after the trading day rolls over and manages the exposure using pip-based risk rules. The StockSharp version mirrors this behaviour with candle subscriptions, `StrategyParam`-driven configuration and explicit per-instrument state tracking.

## Trading idea

1. The user assigns four currency pairs. The first pair must be mapped to the strategy `Security` property; the remaining three are bound through `Symbol2`, `Symbol3` and `Symbol4` parameters.
2. Daily candles feed the most recent and previous day closing prices for each symbol. Intraday candles (1 minute by default) provide the current price and the clock required to detect the midnight window.
3. Five minutes after midnight (inclusive) and until twelve minutes past midnight the strategy checks whether it already opened a basket for the current date. If not, it inspects the sign of the previous daily deviation of the primary symbol (pair 1).
4. A positive previous-day deviation (close<sub>day-1</sub> above close<sub>day-2</sub>) opens three long legs (pairs 1, 2 and 4) and one short leg (pair 3). A negative deviation mirrors the directions. Each leg uses the same `TradeVolume` as in the MQL expert.
5. Throughout the day the strategy monitors the aggregated profit of the four legs expressed in pips. When `ShowProfitInfo` is enabled this value is logged and drives the risk management actions described below.
6. At 23:00 platform time all open legs are closed, matching the original end-of-day behaviour.

## Basket management

- **Emergency doubling** – when `AllowEmergencyTrading` is true, the algorithm arms an emergency flag at the start of each session. If the basket drawdown falls below `-EmergencyLossPips` and at least one leg is profitable, that leg is doubled (same direction, same volume). The routine runs only once per day, exactly like `doubleorders()` in the MQL script.
- **Profit target** – when `UseProfitTarget` is enabled and the aggregated profit reaches `ProfitTargetPips`, all legs are closed.
- **Loss limit** – when `UseLossLimit` is enabled and the aggregated profit drops below `-LossLimitPips`, the basket is closed.
- Risk actions (doubling, take-profit, loss-cut) are executed only when `ShowProfitInfo` is true, faithfully reproducing the gating logic of the source expert.

Every minute the primary instrument logs a "Deviation snapshot" message with:

- Current and previous daily deviations for all four symbols.
- Pair deviations (pairwise sums/differences) exactly as displayed via `Comment()` in the MetaTrader code.
- The latest aggregated pip profit.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `TradeEnabled` | true | Allows opening the daily basket. |
| `ShowProfitInfo` | true | Logs pip profit and activates risk management rules. |
| `UseProfitTarget` | false | Close the basket when total profit ≥ `ProfitTargetPips`. |
| `UseLossLimit` | false | Close the basket when total profit ≤ `-LossLimitPips`. |
| `AllowEmergencyTrading` | true | Enables the emergency doubling routine. |
| `EmergencyLossPips` | 60 | Drawdown threshold (in pips) that triggers doubling. |
| `ProfitTargetPips` | 80 | Positive pip threshold that closes the basket. |
| `LossLimitPips` | 40 | Negative pip threshold that closes the basket. |
| `TradeVolume` | 0.01 | Volume applied to each leg. |
| `Symbol2` | — | Second currency pair (default USDCHF in the EA). |
| `Symbol3` | — | Third currency pair (default GBPUSD in the EA). |
| `Symbol4` | — | Fourth currency pair (default AUDUSD in the EA). |
| `IntradayCandleType` | 1 minute | Candle type used for the time window and current prices. |

## Implementation notes

- Two candle subscriptions are opened per instrument: intraday for time-of-day logic and daily for deviations. All subscriptions use `Bind` to stay within the high-level API guidelines.
- Per-instrument state (`InstrumentState`) stores the latest price, daily closes, average entry price and outstanding volume. This allows the strategy to replicate the pip accounting performed in MQL without accessing indicator internals.
- The aggregated pip profit relies on the instrument `PriceStep` (falling back to `MinPriceStep`) to translate price differences into pips. This mirrors the EA, which divides price deltas by `MODE_POINT`.
- The strategy only reacts to **finished** candles. Intrabar ticks are ignored, consistent with the conversion requirements.
- The emergency flag is reset every time a new basket is opened; once doubling happens the flag is disabled until the next session, mimicking the boolean `enable_emergency_trading` in the original source.
