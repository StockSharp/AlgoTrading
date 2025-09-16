# Auto ADX Strategy

## Overview
The **Auto ADX Strategy** is a direct port of the MetaTrader expert advisor `Auto ADX.mq5` into the StockSharp high-level API. The strategy evaluates Average Directional Index (ADX) strength and the relation between the +DI and -DI components to determine trade direction. It reproduces the original risk controls, including stop-loss, take-profit, reversible signals, and pip-based trailing stops, while adopting StockSharp concepts such as candle subscriptions and indicator bindings.

## Trading Logic
- **Candle Source** – The strategy subscribes to a configurable candle type (default: 1-hour time frame) and processes only finished candles to avoid intrabar noise.
- **ADX Calculation** – A single `AverageDirectionalIndex` indicator is bound through `BindEx`, giving access to the smoothed ADX value as well as the +DI and -DI lines.
- **Long Entry** – Triggered when:
  - +DI is greater than -DI (positive directional momentum),
  - ADX is above the configurable ADX level, and
  - ADX is rising compared to the previous candle.
- **Short Entry** – Triggered when:
  - -DI is greater than +DI (negative directional momentum),
  - ADX is below the configured level, and
  - ADX is falling versus the previous candle.
- **Reverse Mode** – When `ReverseSignals` is enabled (default behaviour), open positions are closed if:
  - A long position sees +DI drop below -DI **or** ADX declines,
  - A short position sees +DI climb above -DI **or** ADX rises.
- **Position Sizing** – Orders are issued with the strategy `Volume`. Reversal handling relies on `ClosePosition()` to exit the entire exposure before a new signal is considered.

## Risk Management
- **Stop-Loss / Take-Profit** – Converted from pip inputs into absolute price distances using the instrument `PriceStep`. StockSharp’s `StartProtection` helper places the protective orders with optional market execution.
- **Trailing Stop** – The original pip-based trailing logic is replicated:
  - Trailing activates only after unrealised profit exceeds the trailing distance.
  - The stop level moves in pip-sized steps (`TrailingStepPips`).
  - A long position exits if price prints below the trailing stop; a short exits when price rallies above the trailing stop.
- **Pip Conversion** – To mimic the MQL implementation, the pip size equals `PriceStep`, multiplied by 10 when the security uses 3- or 5-decimal pricing. This keeps behaviour consistent across forex symbols.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `StopLossPips` | 50 | Distance of the protective stop in pips. Set to zero to disable the stop-loss. |
| `TakeProfitPips` | 50 | Distance of the profit target in pips. Set to zero to disable the take-profit. |
| `TrailingStopPips` | 5 | Size of the trailing stop in pips. Set to zero to disable trailing. |
| `TrailingStepPips` | 5 | Minimal incremental gain (in pips) before the trailing stop is shifted. Must be positive when trailing is enabled. |
| `AdxPeriod` | 14 | Averaging period for the ADX indicator. |
| `AdxLevel` | 30 | ADX strength threshold that filters entries. |
| `ReverseSignals` | true | Enables closing existing positions when the DI relationship or ADX slope flips. |
| `CandleType` | 1 hour | Candle type used for analysis and trading. |

## Implementation Notes
- `BindEx` is used to access the full `AverageDirectionalIndexValue`, ensuring we never rely on manual indicator value retrieval.
- Trailing logic keeps track of the last stop level and moves it only when price progresses by at least `TrailingStepPips` in favour of the position, mirroring the MQL trailing-step behaviour.
- All inline comments in the C# source are in English to satisfy repository guidelines.
- The strategy is self-contained inside `API/2908_Auto_ADX/CS/AutoAdxStrategy.cs`; there is no Python counterpart per requirements.

## Usage Tips
1. Attach the strategy to a security with correct `PriceStep` metadata so pip conversion stays accurate.
2. Adjust `AdxLevel` to match the volatility profile of the traded instrument—higher thresholds reduce signal frequency.
3. When trailing is disabled (`TrailingStopPips = 0`), `TrailingStepPips` is ignored, reproducing the original expert advisor behaviour.
4. Backtest across multiple markets to validate pip-based protection distances and confirm that ADX slope filtering matches expectations.
