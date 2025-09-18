# iVIDyA Simple Strategy

## Overview
This strategy is a high-level StockSharp port of the MetaTrader expert **"iVIDyA Simple"**. It trades a single symbol by tracking a Variable Index Dynamic Average (VIDYA) that adapts to market momentum via the Chande Momentum Oscillator (CMO). Whenever the most recent finished candle crosses the shifted VIDYA line, the strategy opens a market position in the direction of the breakout and optionally attaches protective stop-loss and take-profit orders.

## Trading Logic
1. Candle data is read from the configured timeframe (`CandleType`).
2. The CMO with period `CmoPeriod` is bound to the candle series. Its absolute value dynamically scales the smoothing factor of VIDYA. The base factor equals `2 / (EmaPeriod + 1)` just like the original MQL implementation.
3. A rolling VIDYA value is maintained. On every finished candle the algorithm:
   - Selects the applied price (`AppliedPrice`) from the candle (close, open, median, etc.).
   - Updates the VIDYA with the adaptive smoothing coefficient.
   - Stores historical values to emulate the `MA shift` option from MetaTrader.
4. The candle is compared with the shifted VIDYA value (`MaShift` bars back):
   - If the candle opens below VIDYA and closes above it, a **buy** signal is generated.
   - If the candle opens above VIDYA and closes below it, a **sell** signal is generated.
5. Before opening a new position the strategy flattens any opposite exposure by trading the full volume necessary to reverse.
6. After every entry, `SetStopLoss` and `SetTakeProfit` are called when the respective distances are positive.

This mirrors the original expert advisor which triggered orders strictly on new bars, used a VIDYA calculated from CMO and EMA periods, and attached optional stops expressed in points.

## Parameters
| Name | Default | Description |
|------|---------|-------------|
| `Volume` | `1` | Base trading volume used for orders. Existing exposure is netted automatically when reversing positions. |
| `StopLossPoints` | `150` | Stop-loss distance in price steps. Set to `0` to disable. |
| `TakeProfitPoints` | `460` | Take-profit distance in price steps. Set to `0` to disable. |
| `CmoPeriod` | `15` | Length of the Chande Momentum Oscillator that determines the adaptive VIDYA weight. |
| `EmaPeriod` | `12` | EMA length that defines the base smoothing coefficient in the VIDYA formula. |
| `MaShift` | `1` | Number of completed candles used to shift the VIDYA line forward, matching the `ma_shift` input of the MetaTrader indicator. |
| `AppliedPrice` | `Close` | Price source passed to the VIDYA calculation (`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`). |
| `CandleType` | `TimeSpan.FromMinutes(5)` | Candle type and timeframe used for all calculations and signals. |

## Additional Notes
- Protective orders are managed through the built-in high-level API (`SetStopLoss`/`SetTakeProfit`) while the original MQL code performed manual checks against freeze levels.
- The strategy subscribes to finished candles only, replicating the "new bar" execution constraint from MetaTrader.
- VIDYA history is trimmed automatically so the memory footprint stays small even when `MaShift` is large.
- All comments inside the code are written in English to match the project requirements.
