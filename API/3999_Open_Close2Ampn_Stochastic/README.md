# Open Close2 Ampn Stochastic Strategy

## Overview
- Port of the MetaTrader 4 expert *open_close2ampnstochastic_strategy* rebuilt on top of the StockSharp high-level API.
- Uses a classic Stochastic Oscillator (length 9, smoothing 3/3) together with a two-bar price action filter: the current candle must continue the direction of the previous one before an order is sent.
- Designed for single-position trading. The default candle source is one hour, but any timeframe can be plugged in through the `CandleType` parameter.

## Signal Logic
1. **Entry guard** – only one position can be open at a time. When the strategy is flat it evaluates the last fully formed candle:
   - **Long entry** when the Stochastic main line is above the signal line *and* both the open and close of the latest candle are below their previous values (continuation of downward pressure followed by oscillator strength).
   - **Short entry** when the Stochastic main line is below the signal line *and* the candle shows a higher open and close than the previous one (upward push with bearish oscillator confirmation).
2. **Exit rules** – while a position exists the same conditions are mirrored in reverse:
   - **Close long** when the main line falls below the signal line and the new candle prints higher open/close prices.
   - **Close short** when the main line rises above the signal line and the new candle prints lower open/close prices.
3. **Drawdown guard** – replicates the MT4 emergency exit: if the floating loss magnitude (realised PnL + current candle-based estimate) reaches `MaximumRisk × account_margin`, the strategy liquidates the position immediately. StockSharp does not expose MetaTrader's *AccountMargin*, so the port approximates it via `Portfolio.BlockedValue` and falls back to `Portfolio.CurrentValue` when blocked margin is unavailable.

## Money Management
- **BaseVolume** mirrors the original `Lots` input and is used whenever no account information is available.
- If portfolio valuation exists, the raw order size becomes `Portfolio.CurrentValue × MaximumRisk / 1000`, matching the original `AccountFreeMargin`-based sizing.
- After every losing trade the next position is reduced by `losses / DecreaseFactor`; the streak counter resets after a profitable trade. The resulting size is never allowed to drop below `MinimumVolume`, which defaults to 0.1 lots like the MQL script.
- All calculated volumes are aligned with the instrument limits (`VolumeStep`, `MinVolume`, `MaxVolume`) before sending market orders.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `BaseVolume` | decimal | `0.1` | Fallback order size when risk-based sizing cannot be computed. |
| `MaximumRisk` | decimal | `0.3` | Fraction of equity used both for dynamic sizing and the drawdown guard. Set to `0` to disable risk calculations. |
| `DecreaseFactor` | decimal | `100` | Divisor applied after consecutive losses. Higher values slow down the reduction. |
| `MinimumVolume` | decimal | `0.1` | Absolute floor for the calculated volume. |
| `StochasticLength` | int | `9` | Look-back period of the Stochastic oscillator. |
| `StochasticKLength` | int | `3` | Smoothing period of the %K line. |
| `StochasticDLength` | int | `3` | Smoothing period of the %D signal line. |
| `CandleType` | `DataType` | `TimeFrame(1h)` | Candle source used to drive the indicator and price filters. |

## Implementation Notes
- The floating PnL required by the emergency exit is estimated with the latest candle close and `Strategy.PositionPrice`. This mirrors the intent of `AccountProfit` in MetaTrader, but actual broker-side calculations can differ.
- If neither blocked margin nor portfolio value is exposed by the connector, the drawdown guard remains idle while the strategy still trades using `BaseVolume`.
- `StartProtection()` is enabled on start so that StockSharp's protective mechanisms (stop/take routing, reconnections) mirror the safety net present in the MQL version.

## Differences from the Original Expert
- MetaTrader lot rounding is emulated using the instrument metadata available through StockSharp. Verify the `VolumeStep`/`MinVolume` values for the traded security so the position sizing matches the broker constraints.
- The MT4 code evaluated tick-by-tick while guarding with `Volume[0]`. The port processes only completed candles, which prevents duplicate signals and is the recommended pattern for StockSharp strategies.
- Account metrics are approximations; if you rely on strict margin limits adjust `MaximumRisk` or override the guard to match the broker's exact formulas.
