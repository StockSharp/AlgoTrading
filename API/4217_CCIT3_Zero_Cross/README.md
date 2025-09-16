# CCIT3 Zero Cross Strategy

## Overview
The CCIT3 Zero Cross strategy is a StockSharp port of the MetaTrader 5 expert advisor that trades zero-line reversals of the CCIT3 oscillator. The indicator is built by applying the Tillson T3 smoothing chain to a Commodity Channel Index (CCI). Whenever the smoothed oscillator switches sign the strategy either opens a new position in the direction of the flip or, if configured, closes the current position and reverses it.

## Trading logic
- Calculate the CCI using the selected applied price and period.
- Smooth the oscillator with a Tillson T3 pipeline. Two calculation modes are provided:
  - **Simple** – persistent six-stage smoothing that behaves like the original recalculating MetaTrader indicator.
  - **NoRecalc** – evaluates the T3 polynomial only for the most recent bar, recreating the lightweight “no recalculation” version from the source code.
- When the CCIT3 value crosses from positive to negative, open a long position (or reverse a short if `Trade Overturn` is enabled).
- When the CCIT3 value crosses from negative to positive, open a short position (or reverse a long if `Trade Overturn` is enabled).
- Optional take-profit, stop-loss and trailing stop levels are managed through StockSharp’s `StartProtection` helper.

## Indicators and calculations
- **Commodity Channel Index (CCI)** – runs on the configurable applied price (close, open, high, low, median, typical, weighted) and period.
- **Tillson T3 smoothing** – implemented exactly as in the MQL5 indicator with the `B` volume factor. The Simple mode keeps stateful EMA chains across bars, while NoRecalc recomputes the polynomial from the latest raw CCI reading.
- **Zero-cross detection** – trades are triggered strictly on finished candles, mirroring the original new-bar checks in the expert advisor.

## Risk and position management
- `Take Profit (pts)` and `Stop Loss (pts)` convert into absolute price distances using the instrument’s `PriceStep`.
- `Trailing Stop (pts)` activates StockSharp’s trailing engine with the same point distance.
- `Max Drawdown Target` rescales the base order volume using the current or initial portfolio value (`volume = OrderVolume * balance / target`). Leave the parameter at zero to keep a fixed lot size.
- `Trade Overturn` enables full reversal – the current position is closed first and then a new one is opened in the opposite direction.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `Volume` | 1 | Base order volume before any drawdown scaling. |
| `Take Profit (pts)` | 1750 | Take-profit distance in points. |
| `Stop Loss (pts)` | 0 | Stop-loss distance in points. |
| `Trailing Stop (pts)` | 0 | Trailing stop distance in points (0 disables trailing). |
| `Trade Overturn` | false | Reverse the position on opposite CCIT3 signals. |
| `CCI Period` | 285 | Lookback period for the CCI indicator. |
| `CCI Price` | Typical | Applied price used to feed the CCI. |
| `T3 Period` | 60 | Tillson T3 smoothing length. |
| `T3 Volume Factor` | 0.618 | Tillson T3 `B` coefficient. |
| `Mode` | Simple | CCIT3 calculation mode (`Simple` or `NoRecalc`). |
| `Candle Type` | 1 hour time frame | Timeframe used for candle subscriptions. |
| `Max Drawdown Target` | 0 | Balance divisor for adaptive volume sizing (0 disables scaling). |

## Implementation notes
- The strategy subscribes to a single candle source specified by `Candle Type` and processes only completed candles.
- All volume values are aligned to the security’s volume step and bounded by `VolumeMin`/`VolumeMax`.
- Default parameters replicate the published MT5 configuration: CCIT3 Simple mode with a 285-period CCI, T3 length 60, and 0.618 volume factor.
- Switching to NoRecalc keeps the original indicator’s behaviour of reacting instantly to the raw CCI sign while still producing positive/negative signals.
