# Fluctuate Strategy

The **Fluctuate Strategy** is a StockSharp port of the MetaTrader expert advisor "Fluctuate". It reproduces the original grid-like behaviour using the high-level API: a candle subscription drives all decisions, market entries are performed with `BuyMarket` / `SellMarket`, and recovery orders are placed with stop orders. Long and short exposure are tracked separately to mimic the hedging-style position accounting used in MetaTrader, while the actual StockSharp position remains netted.

## Core idea

1. Every time a new candle closes, the strategy compares the last two close prices. A higher close opens a market buy, a lower close opens a market sell. If both closes are equal the bar is ignored.
2. Each filled position receives a fixed stop-loss and take-profit (expressed in pips). The strategy also records the exact fill price and the net volume added by the trade.
3. After an entry, an **opposite** stop order is armed `StepPips` away from the last fill (plus a small spread buffer). Its volume is derived from the previous trade and the `LotCoefficient`, optionally using the cumulative exposure when `MultiplyLotCoefficient = true`.
4. When the stop order triggers, it cancels the previous pending order, updates the internal exposure statistics and immediately schedules a new recovery stop order in the other direction. This reproduces the averaging / martingale loop present in the MQL implementation.
5. Trailing protection raises (or lowers) the stop once price moves at least `TrailingStopPips + TrailingStepPips` in favour of the position. This emulates the original EA which required an extra profit buffer before tightening the stop.

## Trading workflow

- **Signal detection.** The candle feed is subscribed via `SubscribeCandles`. Only finished candles are processed. The strategy refuses to trade outside the `[StartHour, EndHour)` time window or when the equity guard is triggered.
- **Initial position sizing.** Depending on `PositionSizingMode` the first trade in a sequence either uses a fixed lot (`FixedVolume`) or a risk-based lot (`RiskPercent`). In risk mode the allowed risk (percentage of current equity) is divided by the monetary loss that would occur if the stop-loss is hit. Price step and step-price are used to convert pips to currency.
- **Exposure accounting.** Separate accumulators track long and short volume, average price and the extreme price reached since entry. This allows the strategy to keep both sides "open" internally even though StockSharp uses netting.
- **Recovery orders.** After every fill the algorithm computes the next stop-order volume:
  - When `MultiplyLotCoefficient = false` the new volume equals `LastVolume × LotCoefficient`.
  - When `true` the total absolute exposure is multiplied by `LotCoefficient`.
  - The volume is normalised to exchange constraints (step, min and max volume) and rejected when it would exceed `MaxTotalVolume` or the number of active positions plus orders would exceed `MaxPositions`.
- **Profit target & equity guard.** Aggregated unrealised PnL is calculated by translating price differences into currency using `PriceStep`/`StepPrice`. If it reaches `ProfitTarget`, all positions are closed and pending orders are cancelled. Trading is also suspended when equity drops below `MinEquityPercent` of the initial balance.
- **Trailing logic.** For long positions the highest price seen since entry is recorded. Once it exceeds the entry price by `TrailingStopPips + TrailingStepPips`, a trailing stop is set `TrailingStopPips` behind the high. Short positions apply the symmetric rule with the lowest price. Trailing updates override the fixed stop-loss.

## Risk management details

- **Stop / take profit.** Both are optional (set the pip value to zero to disable). They are recalculated for the aggregated long or short exposure whenever a new trade adds volume.
- **Max positions.** Counts the number of open sides (long + short) plus the active recovery stop order. When the limit is reached, the strategy refuses to submit new stop orders.
- **Max total volume.** Limits the sum of absolute open volume and the volume of the active recovery order.
- **CloseAllAtStart.** Optional safety switch to flatten the book before the strategy starts trading.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `CandleType` | Primary timeframe used for signal detection. | 1-minute time frame |
| `StopLossPips` | Distance between entry price and stop-loss (pips). `0` disables the stop. | 50 |
| `TakeProfitPips` | Distance between entry price and take-profit (pips). `0` disables the take-profit. | 50 |
| `TrailingStopPips` | Trailing stop distance (pips). Requires `TrailingStepPips > 0`. | 5 |
| `TrailingStepPips` | Additional profit needed before the trailing stop advances (pips). | 5 |
| `StepPips` | Distance between the last fill and the opposite recovery stop (pips). | 30 |
| `LotCoefficient` | Multiplier applied to the previous volume (or total exposure). | 2.0 |
| `MultiplyLotCoefficient` | When `true`, the new order volume is computed from the total exposure instead of the last trade. | `false` |
| `MaxPositions` | Maximum number of simultaneous open sides plus the active pending order. | 9 |
| `MaxTotalVolume` | Cap for the sum of open volume and the recovery order volume. | 50 |
| `ProfitTarget` | Unrealised profit (in account currency) that triggers a full exit. `0` disables the target. | 50 |
| `MinEquityPercent` | Minimum equity percentage (vs. starting balance) required to keep trading. Below this threshold only exits are allowed. | 30 |
| `CloseAllAtStart` | Close all positions and cancel orders when the strategy starts. | `false` |
| `StartHour` | Trading window start hour (inclusive, exchange time). | 10 |
| `EndHour` | Trading window end hour (exclusive, exchange time). | 20 |
| `PositionSizingMode` | `FixedVolume` for static lots, `RiskPercent` for percent-of-equity sizing. | `FixedVolume` |
| `VolumeOrRisk` | Fixed lot size (when `FixedVolume`) or risk percentage (when `RiskPercent`). | 1.0 |

## Implementation notes

- Stop-order prices use a minimal spread approximation (`PriceStep` when available) because MetaTrader required the order to be outside the freeze level. Adjust `StepPips` if the actual spread is wider.
- The strategy cancels any remaining recovery order whenever a new trade fills. This matches the original EA which deleted all pending orders after an execution.
- Because StockSharp portfolios are netted, hedged exposure is simulated internally. The actual broker position will always reflect the net quantity.
- Risk-based position sizing requires valid `PriceStep` and `StepPrice` values from the instrument description.

## Usage tips

1. Select an appropriate candle type that matches the original EA testing timeframe (typically M5 or M15) for best fidelity.
2. Double-check exchange volume limits: if the normalised recovery volume becomes zero, the strategy will stop adding new legs.
3. When `PositionSizingMode = RiskPercent`, ensure the portfolio contains up-to-date equity information; otherwise the strategy falls back to the fixed lot size.
4. Combine with StockSharp's built-in `StrategyProtection` (enabled via `StartProtection()`) to add additional account-level safeguards if needed.
