# EA Stochastic Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

High level StockSharp port of the MetaTrader "EA Stochastic" expert advisor. The strategy subscribes to one candle series, reads
stochastic oscillator values, and keeps at most a single net position. Entries occur when the main stochastic line has stayed on the
same side of the configured thresholds for a configurable number of bars. Protective exits and a trailing stop mirror the original
MQL implementation using pip-based distances.

## Strategy Overview

- **Indicator**: classic stochastic oscillator (`%K` and `%D` components with configurable smoothing)
- **Direction**: both long and short
- **Positioning**: single position at a time (new trades are ignored while an exit order is pending)
- **Order type**: market orders using fixed volume
- **Data**: a single user-selected candle type (default 15 minute candles)

## Entry Logic

1. The main stochastic value is stored on every finished candle.
2. After at least `ComparedBar` values are cached, compare the current `kValue` with the value from `ComparedBar - 1` candles ago.
3. **Go Long** when both values are below `UpperLevel`. This matches the original EA that only buys when the oscillator has stayed
   below the upper threshold for the configured lookback length.
4. **Go Short** when both values are above `LowerLevel`. The original EA allowed shorts whenever stochastic stayed above the lower
   bound.
5. New entries are skipped if a position exists or if a protective exit has already been requested for the current position.

## Exit and Risk Management

- **Stop Loss**: optional fixed pip distance from the entry price. Stops are evaluated against candle lows (for longs) or highs
  (for shorts).
- **Take Profit**: optional fixed pip target. High/low checks emulate the MetaTrader order-based take profit behavior.
- **Trailing Stop**: activated once the open trade gains more than `(TrailingStopPips + TrailingStepPips)` pips. The stop is then
  moved to `TrailingStopPips` behind the latest extreme, respecting the trailing step gap just like the original EA.
- **Exit orders**: closes are issued with market orders (`SellMarket` / `BuyMarket`). A guard flag prevents repeated exit orders
  until `OnPositionChanged` confirms the flat state.

## Parameters

- `StopLossPips` (default **50**): pip distance used for the initial protective stop. Set to zero to disable.
- `TakeProfitPips` (default **150**): pip distance for profit taking. Set to zero to disable.
- `TrailingStopPips` (default **15**): trailing distance in pips. Must be greater than zero if trailing is enabled.
- `TrailingStepPips` (default **5**): minimal pip progress required before the trailing stop is updated. Trailing is rejected when
  this value is zero.
- `Volume` (default **1**): market order volume used for both long and short trades.
- `KPeriod` (default **5**): lookback length for the %K stochastic line.
- `DPeriod` (default **3**): smoothing length for the %D line.
- `Slowing` (default **3**): final smoothing applied to the %K calculation.
- `UpperLevel` (default **80**): threshold used to validate long setups.
- `LowerLevel` (default **20**): threshold used to validate short setups.
- `ComparedBar` (default **3**): number of bars to look back when validating stochastic levels (minimum 1).
- `CandleType` (default **15 minute candles**): candle series subscribed by the strategy.

## Implementation Notes

- Pip size is approximated from `Security.PriceStep`. For instruments with fractional pips (typical FX pairs) steps smaller than
  `0.001` are automatically multiplied by 10, reproducing the MetaTrader `digits_adjust` logic.
- Trailing stop configuration is validated on start to avoid the original EA error case (`TrailingStop > 0` with zero trailing
  step).
- The StockSharp stochastic oscillator uses default smoothing and price modes (close/high/low), which maps to the EA settings of
  simple moving average over high/low ranges.
- The original EA provided both fixed lot and risk-percent position sizing. This port keeps the fixed `Volume` parameter and can be
  extended if percentage-based sizing is required.
- Chart output renders the processed candles, stochastic indicator, and executed trades for easier debugging.

## Suggested Usage

- Works on intraday or higher timeframes; adjust `CandleType` and stochastic periods to fit the instrument.
- Tune `UpperLevel`, `LowerLevel`, and `ComparedBar` for different market regimes (range vs. trend).
- Combine with broker-side risk controls in live trading because exits are simulated through market orders after candle
  confirmation.
