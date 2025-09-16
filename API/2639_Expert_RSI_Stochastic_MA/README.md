# Expert RSI Stochastic MA Strategy

## Overview
The **Expert RSI Stochastic MA Strategy** is a conversion of the MetaTrader 5 expert advisor `Expert_RSI_Stochastic_MA.mq5`. The C# implementation leverages StockSharp's high-level strategy API while reproducing the original logic: a trend filter based on a configurable moving average, momentum confirmation from RSI, and a dual-line Stochastic oscillator for precise timing. Protective behaviour replicates the source algorithm with an optional fixed loss threshold and a Stochastic-driven trailing exit.

## Indicators and Parameters
The strategy exposes the same inputs as the MetaTrader version and keeps their default values. All parameters are available for optimisation through the StockSharp UI.

| Category | Parameter | Default | Description |
| --- | --- | --- | --- |
| General | `CandleType` | 15-minute time frame | Candle aggregation used for indicator calculations. |
| Trading | `TradeVolume` | `0.01` | Base order size in lots/contracts. |
| RSI | `RsiPeriod` | `3` | Number of bars used to compute the RSI. |
| RSI | `RsiPriceType` | Close | Applied price for RSI (close, open, high, low, median, typical, weighted). |
| RSI | `RsiUpperLevel` | `80` | Overbought threshold triggering short conditions. |
| RSI | `RsiLowerLevel` | `20` | Oversold threshold triggering long conditions. |
| Stochastic | `StochKPeriod` | `6` | Period of the %K line. |
| Stochastic | `StochDPeriod` | `3` | Period of the %D smoothing line. |
| Stochastic | `StochSlowing` | `3` | Additional slowing factor applied to %K. |
| Stochastic | `StochUpperLevel` | `70` | Overbought level shared by both Stochastic lines. |
| Stochastic | `StochLowerLevel` | `30` | Oversold level shared by both Stochastic lines. |
| Moving Average | `MaMethod` | Simple | Moving average type (simple, exponential, smoothed, weighted). |
| Moving Average | `MaPriceType` | Close | Applied price for the moving average. |
| Moving Average | `MaPeriod` | `150` | Length of the moving average. |
| Moving Average | `MaShift` | `0` | Number of completed bars used to shift the moving average value backwards. |
| Risk | `AllowLossPoints` | `30` | Maximum adverse excursion in points before exiting a losing trade (0 disables). |
| Risk | `TrailingStopPoints` | `30` | Distance in points for the Stochastic-based trailing stop (0 closes on Stochastic without trailing). |

> **Point calculation** – The implementation converts the `AllowLoss` and `TrailingStop` parameters into absolute prices using `Security.PriceStep`. When the instrument has 3 or 5 decimal places the value is multiplied by 10 to emulate MetaTrader's pip handling.

## Trading Logic
### Long Setup
1. **Trend filter** – Candle close must stay above the shifted moving average.
2. **Momentum confirmation** – RSI must be below `RsiLowerLevel`.
3. **Timing** – Both Stochastic lines (%K and %D) must be below `StochLowerLevel`.
4. **Position filter** – Long orders are only placed when no long exposure exists (`Position <= 0`). The order size is `TradeVolume` plus any quantity required to flip an existing short position.

### Short Setup
1. **Trend filter** – Candle close must be below the shifted moving average.
2. **Momentum confirmation** – RSI must exceed `RsiUpperLevel`.
3. **Timing** – Both Stochastic lines must be above `StochUpperLevel`.
4. **Position filter** – New short positions require `Position >= 0`. The strategy offsets existing longs automatically if necessary.

### Exit Management
- **Losing trades**
  - When `AllowLossPoints` is zero the strategy waits for the Stochastic main line to move into the opposite extreme (`StochUpperLevel` for longs, `StochLowerLevel` for shorts) before closing negative trades.
  - When `AllowLossPoints` is positive, the strategy converts the value to a price offset and closes the trade as soon as the loss exceeds this threshold *and* the Stochastic returns inside the neutral zone (`stochMain > StochLowerLevel` for longs, `< StochUpperLevel` for shorts).
- **Trailing exit**
  - With `TrailingStopPoints > 0`, once a trade is profitable and the Stochastic reaches its extreme zone, a trailing stop is set every finished candle. For long trades the stop trails below price; for short trades it trails above.
  - With `TrailingStopPoints = 0`, profitable trades are closed immediately when the Stochastic reaches the extreme level (matching the original EA behaviour).
- **Trailing trigger** – Trailing updates only occur on completed candles, mirroring the MQL implementation that restricted updates to one per bar.

## Implementation Notes
- The moving average shift is handled by storing recent values and reading the value `MaShift` bars back, reproducing MetaTrader's `shift` parameter.
- RSI and moving-average inputs support multiple applied prices to match MetaTrader options. Stochastic calculations rely on StockSharp's built-in oscillator (Low/High mode) and honour the configured smoothing lengths.
- Trailing and loss thresholds are measured in *points*. The helper automatically scales the value for typical FX tick sizes (3 or 5 decimals) and defaults to one `PriceStep` otherwise.
- Chart output includes candles, the moving average, RSI, and Stochastic indicators, allowing visual validation similar to the original template.
- There is no accompanying Python version by request; only the C# implementation is provided.

## Usage Tips
- When deploying on securities with unconventional tick sizes, verify that `Security.PriceStep` is filled; otherwise the default conversion (1 point = 1 price unit) will be used.
- Combine the built-in `StartProtection` or additional risk modules if further stop-loss or take-profit management is required.
- Optimise indicator lengths and risk thresholds together—the strategy intentionally exposes all primary knobs from the MetaTrader expert.
