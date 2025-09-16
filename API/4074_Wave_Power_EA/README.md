# Wave Power EA Strategy

The **Wave Power EA Strategy** is a C# port of the MQL4 expert advisor "Wave Power EA1". The original robot builds a position in
direction of a stochastic or MACD signal and then adds additional market orders every fixed number of pips while adjusting the
shared take-profit level. The StockSharp version reproduces this behaviour using the high-level strategy API, indicator binding
and built-in order helpers. All comments remain in English as required.

## How the strategy works

1. **Signal selection** – the first trade is opened only when one of the indicator filters generates a direction:
   - `Stochastic` – %K crossing %D inside oversold/overbought regions.
   - `MacdSlope` – MACD line rising above or falling below its previous value.
   - `CciLevels` – CCI dropping below –120 or rising above +120.
   - `AwesomeBreakout` – Awesome Oscillator breaking the adaptive historic low/high that was captured during initialisation.
   - `RsiMa` – fast SMA crosses slow SMA while RSI confirms momentum (above/below 50).
   - `SmaTrend` – a 15/20/25/50 SMA fan pointing in the same direction with a minimum slope difference.

2. **Grid expansion** – after the first market order is filled the strategy remembers the fill price. Whenever the market moves
   by `GridStepPips` against the current position and the maximum order count is not exceeded, the strategy submits a new market
   order in the *same* direction. Each new layer multiplies the volume by the `Multiplier` parameter.

3. **Shared targets** – every new order recalculates a common take-profit and (optionally) stop-loss price. When the number of
   active orders approaches the `OrdersToProtect` threshold the take-profit distance is replaced with `ReboundProfitPrimary`.
   After the threshold is exceeded the distance switches to `ReboundProfitSecondary` to encourage faster recovery.

4. **Basket monitoring** – on every candle close the strategy converts the open P&L into pips per lot. If the rebound profit or
   loss protection thresholds are reached the whole basket is liquidated using market orders. The same happens when the oldest
   trade is older than `OrdersTimeAliveSeconds` or when trading on Friday is disabled.

5. **Lifecycle** – once the basket is flat all internal counters are reset, allowing the next signal to start a new averaging
   cycle.

Compared to the original EA this port intentionally avoids opening opposite (hedging) positions after a certain number of grid
layers. All additional entries follow the initial direction. The rest of the money-management rules, protection logic and
indicator filters remain compatible with the MQL4 reference implementation.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `EntryLogic` | Indicator mode used for the very first order. |
| `CandleType` | Timeframe that feeds all indicators (default: 1 hour). |
| `InitialVolume` | Volume of the first order in lots/contracts. |
| `GridStepPips` | Minimal distance in pips between grid layers. |
| `MaxOrders` | Maximum number of simultaneous orders in the basket. |
| `TakeProfitPips` | Shared take-profit distance in pips (0 disables the target). |
| `StopLossPips` | Shared stop-loss distance in pips (0 disables the stop). |
| `Multiplier` | Volume multiplier applied to each additional order. |
| `SecureProfitProtection` | Enables the rebound profit logic. |
| `OrdersToProtect` | Number of orders required before rebound protection starts. |
| `ReboundProfitPrimary` | Profit per lot (in pips) for the first protection stage. |
| `ReboundProfitSecondary` | Profit per lot (in pips) once the protected order count is exceeded. |
| `LossProtection` | Enables the floating-loss guard. |
| `LossThreshold` | Loss per lot (in pips) that triggers the guard when the basket is full. |
| `ReverseCondition` | Inverts buy/sell signals. |
| `TradeOnFriday` | Allows opening new orders on Fridays. |
| `OrdersTimeAliveSeconds` | Maximum lifetime of the newest order in seconds (0 disables the timer). |
| `TrendSlopeThreshold` | Minimal SMA slope difference used by the `SmaTrend` logic. |

## Usage tips

1. Attach the strategy to a security with a configured price step so the pip conversion works correctly.
2. Adjust `GridStepPips`, `Multiplier` and `MaxOrders` according to the instrument volatility and the broker margin policy.
3. Enable the protection blocks when running on a live account to prevent runaway losses during prolonged trends.
4. The strategy relies on closed candles; pick a timeframe that reflects the desired trading rhythm (the original EA uses M30
   and H1 combinations but the default H1 candles work well).
5. Because hedging after the fifth layer is not implemented, consider lowering `MaxOrders` if you require the exact original
   behaviour.

## Files

- `CS/WavePowerEAStrategy.cs` – StockSharp implementation of the Wave Power EA grid logic.
- `README.md` / `README_ru.md` / `README_cn.md` – documentation in English, Russian and Chinese.

The Python version is intentionally omitted per the task requirements.
