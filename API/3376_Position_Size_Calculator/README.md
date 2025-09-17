# Position Size Calculator Strategy

This sample converts the MetaTrader 5 "Position Size Calculator" panel into a StockSharp-only utility.
Instead of opening orders, the strategy continuously evaluates the recommended lot size, risk amount and
margin requirement based on current quotes and the configured risk preferences. All diagnostics are
written into the strategy log so the behaviour can be inspected from the StockSharp UI.

## How it works

1. The strategy subscribes to `Level1` data to obtain the current best bid/ask and last trade prices.
2. Every time a quote update is received, it determines the entry price (best ask for longs, best bid for shorts)
   and derives the stop price from the `Stop Loss (points)` distance.
3. The portfolio value is estimated using the following fallbacks:
   - If `Use Equity` is enabled: `Portfolio.CurrentValue`, then `CurrentBalance`, then `BeginValue`.
   - If `Use Equity` is disabled: `Portfolio.BeginBalance`, falling back to `CurrentBalance`, `BeginValue`, `CurrentValue`.
4. Depending on `Use Risk Money`, the risk budget is taken either from the absolute `Risk Money` parameter or
   calculated as `Risk Percent` of the portfolio value.
5. The stop distance is converted into currency using `Security.PriceStep` and `Security.StepPrice`. The raw lot size is
   normalised to the instrument's volume step and bounded by `MinVolume`/`MaxVolume`.
6. Commission is accounted for by adding both sides of the per-lot commission to the risk amount. Margin is approximated
   from the instrument's `MarginBuy`/`MarginSell` metadata or, if unavailable, from the entry price.
7. When the recommendation changes, the new volume is stored in `Strategy.Volume` and a single informative log entry is
   emitted with entry price, stop price, position size, risk in money and percent, plus the margin estimate.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `Stop Loss (points)` | Stop-loss distance expressed in price steps. |
| `Use Equity` | Switch between equity-based or balance-based capital for the risk calculation. |
| `Use Risk Money` | Toggle between entering the risk as a percent (`false`) or as an absolute amount (`true`). |
| `Risk Percent` | Percentage of capital that can be lost on the trade when `Use Risk Money = false`. |
| `Risk Money` | Currency amount that can be lost on the trade when `Use Risk Money = true`. |
| `Commission per Lot` | One-way commission per lot. The strategy adds commission for entry and exit. |
| `Trade Direction` | Direction used for price selection (buy uses best ask, sell uses best bid). |

## Behavioural notes

- The strategy never sends any orders; it only updates `Strategy.Volume` and logs diagnostics.
- When the computed volume falls below the minimum lot size, the recommendation is discarded to mimic
  the MetaTrader version that leaves the result empty.
- If market data or portfolio information is missing, no log is produced until enough inputs are available.
- The log message mirrors the MetaTrader UI fields: entry price, stop price, lot size, risk in money,
  risk percentage and margin.

## Usage tips

- Attach the strategy to the desired instrument and portfolio. The instrument must provide `PriceStep` and preferably
  `StepPrice` metadata so that the risk conversion is accurate.
- Set `Commission per Lot` according to the broker's schedule to obtain more realistic risk figures.
- Because the strategy keeps all intermediate results in public read-only properties (`RecommendedVolume`,
  `CalculatedRiskMoney`, `CalculatedRiskPercent`, `CalculatedMargin`, `LastEntryPrice`, `LastStopPrice`),
  they can be inspected from custom dashboards or used by parent strategies.

## Differences vs. the MQL version

- The graphical dialog and manual controls are replaced with strategy parameters exposed in StockSharp.
- Risk and margin information is reported via logs instead of on-screen fields.
- Order volume rounding follows StockSharp metadata (`VolumeStep`, `MinVolume`, `MaxVolume`), which matches the
  behaviour of MetaTrader for typical forex and CFD symbols.
