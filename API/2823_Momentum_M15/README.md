# Momentum M15 Strategy

This strategy is a direct port of the MetaTrader 5 expert advisor **Momentum-M15** (original file `Momentum-M15.mq5`).
It trades 15-minute candles and combines a shifted moving average filter with a momentum oscillator that is evaluated on
bar opens. The logic aims to fade extreme momentum when price sits on the opposite side of the shifted average, while a
gap guard and optional trailing stop limit exposure.

## Conversion highlights

* Indicators are recreated with StockSharp components: a configurable moving average (default smoothed) and the built-in
  `Momentum` oscillator working on the chosen candle price (default `Open`).
* The MetaTrader horizontal MA shift is emulated by buffering indicator values and retrieving the value `MaShift`
  finished bars back. No custom indicator math is reimplemented.
* Momentum monotonicity checks reuse the latest history values and keep only as many elements as required by the entry
  or exit windows, mirroring the original `CheckMO_Up` / `CheckMO_Down` helpers.
* The large-gap lockout (`GapLevel`/`GapTimeout`) is preserved. Price step information is used to convert the point-based
  thresholds defined in the MQL version into StockSharp price steps.
* Trailing stop management is handled internally through market exits when price crosses the tracked level, matching the
  MQL routine that modified stop-loss orders once per completed bar.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `TradeVolume` | Order size used for every entry. | `0.1` |
| `CandleType` | Primary timeframe (15-minute candles by default). | `15m` |
| `MaPeriod` | Lookback length of the moving average filter. | `26` |
| `MaShift` | Number of bars to shift the moving average horizontally. | `8` |
| `MaMethod` | Moving average type (`Simple`, `Exponential`, `Smoothed`, `Weighted`). | `Smoothed` |
| `MaPrice` | Candle price fed to the moving average. | `Low` |
| `MomentumPeriod` | Momentum lookback length. | `23` |
| `MomentumPrice` | Candle price used for the momentum oscillator. | `Open` |
| `MomentumThreshold` | Baseline momentum level that separates long/short setups. | `100` |
| `MomentumShift` | Value added/subtracted from `MomentumThreshold` to build asymmetric bounds. | `-0.2` |
| `MomentumOpenLength` | Bars required for a non-increasing momentum sequence before opening longs / non-decreasing for shorts. | `6` |
| `MomentumCloseLength` | Bars required for the same monotonic sequence before closing positions. | `10` |
| `GapLevel` | Minimum positive gap (in price steps) that pauses new entries. | `30` |
| `GapTimeout` | Number of bars to keep trading disabled after a large gap. | `100` |
| `TrailingStop` | Optional trailing-stop distance measured in price steps. | `0` (disabled) |

## Trading rules

### Entry conditions

* **Long entries**
  * Latest momentum is below `MomentumThreshold + MomentumShift` (for the default shift of `-0.2`, this is slightly
    below the main threshold).
  * Both the previous bar close and the current bar open are **below** the shifted moving average.
  * Momentum has been non-increasing for `MomentumOpenLength` bars (matching `CheckMO_Down` in the MQL source).

* **Short entries**
  * Latest momentum is above `MomentumThreshold - MomentumShift` (with the default shift this is slightly above 100).
  * Both the previous bar close and the current bar open are **above** the shifted moving average.
  * Momentum has been non-decreasing for `MomentumOpenLength` bars (matching `CheckMO_Up`).

Entries are only evaluated when no position is open and trading is not suspended by the gap filter.

### Exit conditions

* **Long positions** close when either of the following is true:
  * Momentum has been non-increasing for `MomentumCloseLength` bars.
  * The previous bar close drops below the shifted moving average.
  * Trailing stop (if enabled) is touched. The stop trails the candle low minus the configured distance expressed in
    price steps.

* **Short positions** close when either of the following is true:
  * Momentum has been non-decreasing for `MomentumCloseLength` bars.
  * The previous bar close rises above the shifted moving average.
  * Trailing stop (if enabled) is touched. The stop trails the candle high plus the configured distance.

### Gap suspension logic

The original expert advisor paused trading after strong upward gaps. The StockSharp version measures the difference
between the current bar open and the previous close in price steps:

1. When the gap exceeds `GapLevel`, the lockout timer is reset to `GapTimeout`.
2. The timer is decremented every closed bar; trading resumes only after it reaches zero.

## Notes and assumptions

* All calculations use finished candles (`CandleStates.Finished`) to stay aligned with StockSharp high-level API
  practices. As a result, orders are emitted at the next bar after conditions are observed, which is consistent with how
  the original strategy triggered on the first tick of a new bar.
* The MetaTrader concept of “pips” is approximated via `Security.PriceStep`. If the instrument lacks proper step data,
  the gap filter and trailing stop will silently disable themselves.
* Moving average prices and momentum inputs can be changed independently, replicating the flexibility of the original
  input parameters.
* No automated stop orders are registered; instead, market exits reproduce the stop adjustments that the MQL code issued
  through `PositionModify`.

## Usage tips

1. Assign the desired security and ensure the `CandleType` matches the historical timeframe used during backtests (15
   minute bars in the original script).
2. Configure `TradeVolume` to the lot size supported by the trading venue.
3. Tune `MomentumOpenLength` / `MomentumCloseLength` to control how strict the momentum monotonicity filter should be.
4. If you prefer to mirror the default “pip” scale exactly, set `TrailingStop` and `GapLevel` according to the ratio
   between the exchange’s price step and one pip for the instrument.
