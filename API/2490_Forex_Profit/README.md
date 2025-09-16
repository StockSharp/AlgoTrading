# Forex Profit Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Translation of the "Forex Profit" MetaTrader expert advisor. The strategy waits for alignment of three exponential moving averages and confirmation from Parabolic SAR before entering trades on the close of each finished candle. Risk is controlled through asymmetric stop-loss and take-profit distances, a trailing stop and an additional EMA-based profit lock.

## Details

- **Entry Criteria**:
  - Long: `EMA10` above both `EMA25` and `EMA50`, previous bar's `EMA10` at or below `EMA50`, and Parabolic SAR below the prior close.
  - Short: `EMA10` below both `EMA25` and `EMA50`, previous bar's `EMA10` at or above `EMA50`, and Parabolic SAR above the prior close.
  - Signals are evaluated only once per completed candle.
- **Exit Criteria**:
  - Close long when `EMA10` turns below its previous value *and* current profit exceeds the `ProfitThreshold`.
  - Close short when `EMA10` turns above its previous value *and* current profit exceeds the `ProfitThreshold`.
  - Protective stop-loss and take-profit levels set at order entry (different distances for longs vs shorts).
  - Trailing stop activates after price moves `TrailingStopPoints` beyond the entry and is updated by `TrailingStepPoints` increments.
- **Stops**: Yes — fixed stop-loss, fixed take-profit, and trailing stop management.
- **Default Values**:
  - `FastEmaLength` = 10
  - `MediumEmaLength` = 25
  - `SlowEmaLength` = 50
  - `TakeProfitBuyPoints` = 55
  - `TakeProfitSellPoints` = 65
  - `StopLossBuyPoints` = 60
  - `StopLossSellPoints` = 85
  - `TrailingStopPoints` = 74
  - `TrailingStepPoints` = 5
  - `ProfitThreshold` = 10
  - `SarAcceleration` = 0.02
  - `SarMaxAcceleration` = 0.2
  - `Volume` = 1
  - `CandleType` = 1 hour timeframe
- **Additional Notes**:
  - Stop/target distances are expressed in instrument price steps and converted automatically using the security's tick size.
  - Profit-based exits rely on the total position profit (including volume) converted from price ticks to account currency.
  - Trailing logic keeps the stop behind price swings without overshooting the configured step.
- **Filters**:
  - Category: Trend following
  - Direction: Long & Short
  - Indicators: EMA, Parabolic SAR
  - Stops: Yes (fixed + trailing)
  - Complexity: Intermediate
  - Timeframe: Configurable (default 1 hour)
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
