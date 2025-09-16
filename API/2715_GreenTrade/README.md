# GreenTrade Strategy

## Overview
The GreenTrade strategy is a conversion of the original MQL5 expert advisor. It follows medium-term trends by combining a smoothed moving average (SMMA) slope filter with momentum confirmation from the Relative Strength Index (RSI). Signals are calculated on completed candles of the configured timeframe, and the strategy can pyramid up to a configurable number of position units while applying fixed risk controls and a step-based trailing stop.

## Trading Logic
1. **Indicator preparation**
   - SMMA is calculated on the median price `((High + Low) / 2)` using the `MaPeriod` parameter.
   - RSI is calculated on the closing price with the `RsiPeriod` lookback.
2. **Trend shape filter**
   - Four historical SMMA samples are inspected according to the bar shift parameters (`ShiftBar`, `ShiftBar1`, `ShiftBar2`, `ShiftBar3`).
   - A bullish trend requires `SMMA(shift0) > SMMA(shift1) > SMMA(shift2) > SMMA(shift3)`.
   - A bearish trend requires `SMMA(shift0) < SMMA(shift1) < SMMA(shift2) < SMMA(shift3)`.
3. **Momentum confirmation**
   - RSI must be above `RsiBuyLevel` for long entries and below `RsiSellLevel` for short entries. The RSI value is taken at `ShiftBar` bars back to mirror the MQL5 logic that ignores the forming candle.
4. **Order execution**
   - When a signal is confirmed and the position cap is not exceeded, the strategy sends a market order for `TradeVolume`.
   - If a position exists in the opposite direction, the strategy first neutralizes it and then opens a new position with the configured volume.
   - If a position exists in the same direction, the trade volume is added to the net exposure up to `MaxPositions * TradeVolume`.

## Risk Management
- **Initial Stop Loss / Take Profit**: Each new entry sets price targets based on `StopLossPips` and `TakeProfitPips`. Pip distances are converted to price units using the security `PriceStep`. Instruments with fractional steps (e.g., five-digit Forex symbols) receive an extra factor of 10 just like the original expert.
- **Trailing Stop**: When profit exceeds `TrailingStopPips + TrailingStepPips`, the stop is moved to maintain a distance of `TrailingStopPips`. Additional moves require another `TrailingStepPips` of price improvement, reproducing the stepwise trailing behavior from the MQL code.
- **Position Cap**: The `MaxPositions` parameter limits the maximum number of volume units. Signals that would exceed this cap are ignored.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `MaPeriod` | Length of the smoothed moving average applied to the median price. | 67 |
| `ShiftBar`, `ShiftBar1`, `ShiftBar2`, `ShiftBar3` | Offsets (in bars) used to access historical SMMA samples for the trend shape filter. | 1, 1, 2, 3 |
| `RsiPeriod` | Lookback period for the RSI indicator. | 57 |
| `RsiBuyLevel` | RSI threshold that confirms bullish setups. | 60 |
| `RsiSellLevel` | RSI threshold that confirms bearish setups. | 36 |
| `TradeVolume` | Volume applied to each entry or add-on. | 0.1 |
| `StopLossPips` | Distance for the initial stop loss in pips (0 disables it). | 300 |
| `TakeProfitPips` | Distance for the initial take profit in pips (0 disables it). | 300 |
| `TrailingStopPips` | Distance between price and trailing stop once activated (0 disables trailing). | 12 |
| `TrailingStepPips` | Additional progress required before the trailing stop is moved again. | 5 |
| `MaxPositions` | Maximum number of volume units (`TradeVolume` multiples) that can be active. | 7 |
| `CandleType` | Candle data series used for indicator updates. | 1-hour timeframe |

## Notes
- All calculations are performed on completed candles only; unfinished candles are ignored to avoid noisy signals.
- Position state is tracked internally so that stop-loss, take-profit, and trailing exits are handled even when protective orders are not placed at the exchange.
- The conversion retains the original behavior for pip conversion and trailing step logic, while leveraging the StockSharp high-level API for subscriptions and order execution.
