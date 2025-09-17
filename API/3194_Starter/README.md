# Starter Strategy

This strategy ports the MetaTrader expert **Starter.mq5** to StockSharp's high level API. It synchronises three stochastic
oscillators (fast, normal, slow) with matching moving averages calculated on different timeframes. A trade is allowed only
when all filters confirm the same direction and price trades on the correct side of every shifted moving average.

## Trading logic

1. The strategy subscribes to three candle streams:
   - **Fast timeframe** (default `M5`).
   - **Normal timeframe** (default `M30`).
   - **Slow timeframe** (default `H2`).
2. For each stream it builds a moving average (configurable method, length, and applied price) and a stochastic oscillator
   with the same `%K`, `%D`, and slowing parameters.
3. The slow timeframe drives execution. When a slow candle closes, the latest values from all timeframes are compared:
   - Long setup: every stochastic line has `%K > %D`, all `%K` values are below `50`, and price is below every shifted
     moving average.
   - Short setup: every stochastic line has `%K < %D`, all `%K` values are above `50`, and price is above every shifted
     moving average.
4. Signals can optionally be inverted through `ReverseSignals`. When an entry is taken the strategy either flips the
   existing exposure (if `CloseOppositePositions = true`) or ignores the signal until the opposite position is closed.
5. After a fill, stop-loss and take-profit levels are simulated in price space. A trailing stop replicates the original MQL
   logic by requiring `TrailingStopPips + TrailingStepPips` of profit before moving the stop by `TrailingStopPips`.
6. Risk-based position sizing mirrors MetaTrader's `lot`/`risk` switch. When the mode is `RiskPercent`, the trade volume is
   derived from the account value, risk percentage, and the stop-loss distance in pips.

## Parameters

| Name | Default | Description |
|------|---------|-------------|
| `StopLossPips` | `45` | Protective stop distance in pips. Set to `0` to disable the fixed stop. |
| `TakeProfitPips` | `105` | Take-profit distance in pips. Set to `0` to disable the target. |
| `TrailingStopPips` | `5` | Trailing stop offset applied after the minimum advance. |
| `TrailingStepPips` | `5` | Minimum profit advance (in pips) required before the trailing stop moves. |
| `MoneyMode` | `RiskPercent` | Selects between fixed lot sizing and percentage risk per trade. |
| `MoneyValue` | `3` | Lot size when using `FixedLot`, or percentage risk when using `RiskPercent`. |
| `FastCandleType` | `M5` | Candle type for the fast indicator set. |
| `NormalCandleType` | `M30` | Candle type for the intermediate indicator set. |
| `SlowCandleType` | `H2` | Candle type that triggers signal evaluation and orders. |
| `MaPeriod` | `20` | Length of all moving averages. |
| `MaShift` | `1` | Horizontal shift applied to every moving average (bars back). |
| `MaMethod` | `Simple` | Moving average smoothing: `Simple`, `Exponential`, `Smoothed`, or `Weighted`. |
| `MaPriceType` | `Close` | Applied price used to feed the moving averages. |
| `StochasticKPeriod` | `5` | `%K` length for all stochastic oscillators. |
| `StochasticDPeriod` | `3` | `%D` smoothing length. |
| `StochasticSlowing` | `3` | Final slowing factor for `%K`. |
| `ReverseSignals` | `false` | Swaps the long and short conditions. |
| `CloseOppositePositions` | `false` | If `true`, reverses the position in a single order when a signal appears in the opposite direction. |

## Money management

- `MoneyMode = FixedLot` sends every order with the exact `MoneyValue` volume.
- `MoneyMode = RiskPercent` reproduces the original behaviour: the risked cash equals `AccountValue * MoneyValue / 100`. The
  trade size is calculated as `risked cash / (StopLossPips * pip size)`. If `StopLossPips` is zero or the portfolio value is
  not available, the strategy refuses to trade.

## Protection and trailing

- Stop-loss and take-profit levels are tracked internally and compared against candle highs/lows, emulating MetaTrader's
  protective orders.
- The trailing stop activates only after the unrealised profit exceeds `TrailingStopPips + TrailingStepPips` pips, matching
  the original requirement that both an initial offset and a minimum step must be satisfied before the stop is moved.

## Multi-timeframe alignment

All indicators are recalculated on every closed candle of their respective timeframe. The slow timeframe waits for all three
moving averages and stochastics to form and uses the most recent shifted moving average values, mimicking the MetaTrader
`iMA` shift parameter. This ensures that the StockSharp port triggers trades on the same bar as the original MQL expert.
