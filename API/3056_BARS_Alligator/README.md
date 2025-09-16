# BARS Alligator Strategy

The BARS Alligator strategy is a direct port of the MetaTrader expert advisor with the same name. It relies on Bill Williams'
Alligator indicator to detect awakening trends: when the green lips line crosses above the blue jaw line the system treats it as
a bullish breakout, while a downward cross signals bearish momentum. Exits rely on the lips crossing the red teeth line so that
positions are closed as soon as momentum fades. Protective stop-loss, take-profit and trailing-stop distances are configured in
pips and automatically converted to price units based on the instrument's price step and decimal precision.

## Trading Logic

1. **Indicator construction**
   - Three moving averages with configurable lengths, shifts and type (simple, exponential, smoothed or weighted) form the Alligator.
   - The applied price can be the close, open, high, low, median, typical or weighted price of each candle.
   - Shifts are respected by storing a small rolling buffer for each line so that crossovers use the same values that would appear
     on a MetaTrader chart.
2. **Entry conditions**
   - **Long**: the lips line on the previous bar is above the jaw and was below it two bars ago (bullish cross upward).
   - **Short**: the lips line on the previous bar is below the jaw and was above it two bars ago (bearish cross downward).
   - New entries are allowed only if the current position is flat or already aligned with the signal direction and the aggregate
     position size remains below `MaxPositions × OrderVolume` (or the risk-sized equivalent).
3. **Exit conditions**
   - **Long exit**: the lips line crosses below the teeth line and the position is in profit relative to the averaged entry price.
   - **Short exit**: the lips line crosses above the teeth line and the position is profitable.
   - Exits also occur when static stop-loss or take-profit levels are breached.
4. **Trailing stop**
   - When enabled, a trailing stop repositions the protective stop once price moves beyond `TrailingStopPips + TrailingStepPips`
     in the trade direction. The stop then follows price at `TrailingStopPips` pips distance but only advances if price makes new
     progress of at least `TrailingStepPips` pips.
5. **Money management**
   - With `MoneyMode = FixedVolume`, orders use the `OrderVolume` size directly.
   - With `MoneyMode = RiskPercent`, the strategy allocates volume so that the configured `MoneyValue` percent of portfolio equity
     would be lost if the stop-loss were hit. The per-unit risk equals the stop-loss distance expressed in price units. The result
     is rounded down to the nearest `VolumeStep` (or to 1 when step information is missing).

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `CandleType` | `DataType` | `TimeSpan.FromHours(1).TimeFrame()` | Timeframe used for Alligator calculations. |
| `OrderVolume` | `decimal` | `0.1` | Fixed trade volume when `MoneyMode` is `FixedVolume`. |
| `MoneyMode` | `MoneyManagementMode` | `FixedVolume` | Chooses between fixed volume and risk-percent sizing. |
| `MoneyValue` | `decimal` | `1` | Risk percentage applied when `MoneyMode` is `RiskPercent`; ignored otherwise. |
| `MaxPositions` | `int` | `1` | Maximum number of additive entries per direction (expressed as multiples of the calculated order volume). |
| `StopLossPips` | `int` | `150` | Stop-loss distance in pips. Zero disables the protective stop. |
| `TakeProfitPips` | `int` | `150` | Take-profit distance in pips. Zero disables the profit target. |
| `TrailingStopPips` | `int` | `5` | Trailing stop distance in pips. Zero disables trailing. |
| `TrailingStepPips` | `int` | `5` | Extra distance price must travel before the trailing stop advances. Must be positive when trailing is enabled. |
| `JawPeriod` | `int` | `13` | Length of the jaw moving average. |
| `JawShift` | `int` | `8` | Forward shift (in bars) applied to the jaw series. |
| `TeethPeriod` | `int` | `8` | Length of the teeth moving average. |
| `TeethShift` | `int` | `5` | Forward shift applied to the teeth series. |
| `LipsPeriod` | `int` | `5` | Length of the lips moving average. |
| `LipsShift` | `int` | `3` | Forward shift applied to the lips series. |
| `MaType` | `MovingAverageType` | `Smoothed` | Moving-average algorithm used for all three Alligator lines. |
| `AppliedPrice` | `AppliedPriceType` | `Median` | Candle price supplied to the moving averages (close, open, high, low, median, typical or weighted). |

### Pip conversion

The strategy multiplies pip settings by the security `PriceStep`. When the instrument uses 3 or 5 decimals the value is adjusted by
×10 to mimic MetaTrader's pip definition for fractional quotes. If no price step is available, a value of 1 is assumed.

## Implementation Notes

- `MaxPositions` acts on the aggregated position size because StockSharp operates in netting mode. Additional entries increase the
  average price instead of creating separate position tickets.
- The stop-loss and take-profit are tracked internally and executed with market orders on the first candle that violates the
  thresholds, matching the behaviour of the original MQL expert.
- Risk-based sizing requires a non-zero stop-loss distance; otherwise the system falls back to the fixed `OrderVolume`.
- All indicator values are updated only on finished candles (`CandleStates.Finished`) to avoid premature signals.
