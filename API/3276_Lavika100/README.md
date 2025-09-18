# Lavika100 Strategy (StockSharp)

## Overview
The **Lavika100 Strategy** is a faithful port of the MetaTrader 5 expert advisor "Lavika  cent". The system combines a one-hour (H1) and a four-hour (H4) RAVI momentum filter to decide when to open trades. It keeps the original money management choices (fixed lot or risk percentage), one-position discipline, optional signal reversal and automatic stop management. The StockSharp version adheres to the high level API guidelines: candle subscriptions drive the workflow, indicators are accessed through binders, and protective orders are configured with `StartProtection`.

## Workflow
1. **Data subscriptions** – the strategy subscribes to H1 candles for the execution timeframe and H4 candles for the trend filter. The `SimpleMovingAverage` indicator is applied to the open prices to emulate the MT5 `iMA(..., PRICE_OPEN)` calls.
2. **RAVI momentum** – two moving averages on every timeframe (fast/slow) generate a "RAVI" percentage: `(fast - slow) / slow * 100`. The H1 value needs to be positive before any trade is considered.
3. **Trend pattern detection** – the most recent four RAVI values on H4 are inspected:
   - A rising sequence (`r0 > r1`, `r1 < r2`, `r2 < r3`) triggers a long signal.
   - A falling sequence (`r0 < r1`, `r1 > r2`, `r2 > r3`) triggers a short signal. This mirrors the behaviour of the original code even though the expert only flipped direction via the `Reverse` flag.
4. **Signal reversal and flattening** – depending on the `ReverseSignals` and `CloseOpposite` parameters the algorithm either opens in the detected direction or reverses it, closing any opposite position beforehand.
5. **Money management** – volume is taken from `FixedVolume` or scaled by risk via the `RiskPercent` method (portfolio value * percent / stop distance).
6. **Protection** – stop-loss, take-profit, trailing stop and trailing step are activated via `StartProtection` as soon as the strategy starts and the parameters are non-zero.

## Trading Rules
- **Long entry** – H1 RAVI is positive and the H4 series shows a rising pattern. The strategy closes an existing short position when `CloseOpposite=true` before buying.
- **Short entry** – H1 RAVI is positive and the H4 series shows a falling pattern. When `ReverseSignals=true` the directions swap, matching the MT5 "Reverse" toggle.
- **Single position** – with `OnlyOnePosition=true` any non-flat exposure blocks additional entries until the position is closed.
- **Volume sizing** – risk-percentage mode uses the instrument `PriceStep`/`StepPrice` pair to convert price distance to monetary value, respecting `VolumeStep`, `VolumeMin` and `VolumeMax`.

## Parameters
| Name | Description |
| --- | --- |
| `H1CandleType` | Timeframe for the execution logic (default 1 hour). |
| `H4CandleType` | Higher timeframe used by the trend filter (default 4 hours). |
| `H1FastPeriod` / `H1SlowPeriod` | Moving average lengths for the H1 RAVI. |
| `H4FastPeriod` / `H4SlowPeriod` | Moving average lengths for the H4 RAVI. |
| `StopLossPoints` | Stop-loss distance in pip-based points. |
| `TakeProfitPoints` | Take-profit distance in pip-based points. |
| `TrailingStopPoints` | Trailing stop distance. Set to zero to disable trailing. |
| `TrailingStepPoints` | Minimum step for trailing updates. Must be positive when trailing is enabled. |
| `FixedVolume` | Lot size used in fixed mode. |
| `RiskPercent` | Percent of portfolio value to risk when `MoneyMode` equals `RiskPercent`. |
| `MoneyMode` | Switch between `FixedLot` and `RiskPercent`. |
| `OnlyOnePosition` | Allow only a single open position. |
| `ReverseSignals` | Flip long/short actions (default true to match the EA setting). |
| `CloseOpposite` | Close an opposite position before placing a new order. |

## Conversion Notes
- Pip conversion mimics the MT5 expert: three- and five-digit quotes multiply `PriceStep` by ten to obtain a pip-sized increment.
- The RAVI history is stored without custom collections—just four nullable fields—which respects the repository restrictions against manual buffers.
- Money management avoids indicator `GetValue` calls and uses StockSharp market metadata to map percentage risk to volume.
- `StartProtection` is only called when at least one of the protective distances is positive, ensuring safe execution during backtests and live trading.

## Usage Tips
- Provide a Forex-style instrument with correctly configured `PriceStep`, `StepPrice`, `VolumeStep`, `VolumeMin` and `VolumeMax`.
- When using risk-based sizing, define a non-zero `StopLossPoints`; otherwise the calculated volume will be zero.
- Because the original EA contained a logic quirk where both patterns set the buy flag, keep `ReverseSignals=true` if you need to reproduce its exact trades.
