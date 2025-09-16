# Ivan CCI Averaging Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Port of the "Ivan" MetaTrader expert advisor that trades CCI extremes with averaging entries and a smoothed moving-average stop. The strategy monitors a long-term CCI(100) to raise global buy or sell regimes, optionally layers additional positions when a short-term CCI(13) retraces, and manages risk with break-even and trailing logic around a smoothed moving average. Position sizing mirrors the original percent-risk model and a profit-protection coefficient closes the book when equity multiplies.

## Details

- **Entry Criteria**:
  - **Long global signal**: CCI(100) rises above `GlobalSignalLevel` while no buy regime is active. A long market order is sent with the initial stop at the smoothed MA value, provided the stop is at least `MinStopDistance` below price.
  - **Long averaging**: If `UseAveraging` is enabled and the global buy flag is set, any dip of CCI(13) below `-GlobalSignalLevel` adds another long using the same stop template.
  - **Short global signal**: CCI(100) falls below `-GlobalSignalLevel` while no sell regime is active, triggering a short entry when the MA stop is at least `MinStopDistance` above price.
  - **Short averaging**: With `UseAveraging` enabled, a rally of CCI(13) above `GlobalSignalLevel` inside a sell regime adds to the short exposure.
- **Long/Short**: Trades both directions and can pyramid positions inside the active bias.
- **Exit Criteria**:
  - Crossing back inside `±ReverseLevel` on CCI(100) cancels both regimes and forces flat exposure.
  - Portfolio equity exceeding `ProfitProtectionFactor` times the starting balance forces liquidation of all positions.
  - Reaching the tracked stop price (break-even or trailed MA) closes the position side.
- **Stops**:
  - Initial stop comes from a `StopLossMaPeriod` smoothed moving average (SMMA).
  - Break-even moves the stop to the entry price once price advances by `BreakEvenDistance` (set to zero to disable).
  - Trailing tightens the stop only if the MA progresses by at least `TrailingStep` beyond the current stop.
- **Filters**:
  - `UseZeroBar` replicates the MT5 option to read either the freshly opened bar or the last closed bar for signal comparisons.
  - `MinStopDistance` prevents trades when the MA stop is too close to price.
- **Position Sizing**:
  - Each new order risks `RiskPercent` of the current portfolio value divided by the distance between price and the stop, with `MinimumVolume` as a safety floor.

## Parameters

- **Use Averaging** *(bool, default: true)* — Enable additional averaging orders during an active global regime.
- **Stop MA Period** *(int, default: 36)* — Period of the smoothed MA used to derive stop levels.
- **Risk %** *(decimal, default: 10)* — Percentage of account equity to risk on each new trade.
- **Use Zero Bar** *(bool, default: true)* — If true, uses the latest candle values; otherwise signals rely on the previous closed bar.
- **Reverse Level** *(decimal, default: 100)* — Absolute CCI threshold that cancels both regimes and closes all positions.
- **Global Level** *(decimal, default: 100)* — Absolute CCI threshold that activates a new global buy or sell signal.
- **Min Stop Distance** *(decimal, default: 0.005)* — Minimum price gap between entry and the MA stop (0.005 ≈ 50 pips on 5-digit FX pairs).
- **Trailing Step** *(decimal, default: 0.001)* — Minimum improvement required before the MA trailing stop is advanced.
- **BreakEven Distance** *(decimal, default: 0.0005)* — Price move needed to shift the stop to the entry price; set to 0 to disable break-even.
- **Profit Protection** *(decimal, default: 1.5)* — Equity multiple that triggers full liquidation to lock in gains.
- **Minimum Volume** *(decimal, default: 1)* — Fallback trade size when risk-based sizing yields small or zero volume.
- **Candle Type** *(DataType)* — Candle series used for indicators (default 15-minute time frame).

## Notes

- Distances such as `MinStopDistance`, `TrailingStep`, and `BreakEvenDistance` are expressed in price units and should be adjusted to the instrument's tick size.
- The strategy assumes synchronous fills from `BuyMarket`/`SellMarket` orders; adjust execution settings if slippage or partial fills are expected.
- Portfolio-based sizing requires a connected portfolio adapter; otherwise `MinimumVolume` is used for all orders.
