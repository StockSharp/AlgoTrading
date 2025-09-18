# Bago EA Classic Strategy

This strategy is a faithful StockSharp port of the MetaTrader expert from `MQL/7656/Bago_ea.mq4`. It keeps the original trend-following philosophy: entries are triggered only when exponential moving averages and RSI break the neutral zone in the same direction, while the Vegas tunnel acts as a spatial filter and as the anchor for step-by-step trailing.

## Trading Logic in Detail

1. **Indicator stack**
   - Fast and slow EMAs (`FastPeriod`/`SlowPeriod`, shared method `MaMethod`, applied price `MaAppliedPrice`).
   - Vegas tunnel EMAs with fixed periods 144 and 169 using the same settings to emulate the tunnel envelopes.
   - RSI (`RsiPeriod`, `RsiAppliedPrice`) with the classic 50 level used as a confirmation filter.
   - Candle data comes from `CandleType`; the same candle feed powers all indicators through the high-level `Bind` pipeline.
2. **Cross-state machine**
   - EMA and RSI crossings above/below their thresholds set boolean flags and bar counters. Each state expires after `CrossEffectiveBars` completed candles or when the opposite cross appears, exactly like the timers from the MQL version.
   - Additional tunnel flags track when the close price jumps from one side of the Vegas tunnel to the other so the trailing logic knows which regime to apply.
3. **Session gate**
   - Trading is permitted only during selected market sessions: London (07–16), New York (12–21) and Tokyo (00–08 plus the 23:00 bar). These windows replicate the `extern bool` switches in the original EA.
4. **Entry filters**
   - **Long**: requires both EMA-up and RSI-up flags and either a bullish close above the tunnel by at least `TunnelBandWidthPips` but not further than `TunnelSafeZonePips`, or a retracement close below the tunnel by `TunnelBandWidthPips` signalling a bounce.
   - **Short**: mirrored conditions using EMA-down/RSI-down and symmetrical tunnel checks.
   - When a reverse position is open the strategy closes it at market before entering the new direction, mimicking `OrderClose` from MetaTrader.
5. **Position and exit management**
   - Initial stop-loss is placed `StopLossPips` away from the entry. Whenever the price parks around the Vegas tunnel the stop is relocated using an extra cushion `StopLossToFiboPips` to match the "fibo" offsets of the expert.
   - Trailing steps correspond to the TP levels from the EA. As price moves away from the tunnel the strategy first parks the stop near tunnel ±(`TrailingStepX` + `StopLossToFiboPips`) and gradually switches to a pure price-following trailing of `TrailingStopPips`.
   - Partial exits (`PartialClose1Volume`, `PartialClose2Volume`) are executed once the move reaches `TrailingStep1Pips` and `TrailingStep2Pips`. Remaining volume is managed by the trailing stop until the third step (`TrailingStep3Pips`) is hit.
   - Any opposite EMA/RSI cross immediately closes the full position.
6. **Order handling**
   - Stop orders are maintained explicitly via `SellStop`/`BuyStop` calls. Each time the stop needs to move the previous order is cancelled and a new one is submitted; this mirrors the repeated `OrderModify` calls from the MQL code.
   - All pip calculations rely on the instrument `PriceStep` and automatically adjust for 3- or 5-digit quotes by multiplying the step by ten, just like MetaTrader's point conversion.

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `TradeVolume` | decimal | 3 | Total volume opened on a new signal. |
| `StopLossPips` | decimal | 30 | Initial protective stop distance in pips. |
| `StopLossToFiboPips` | decimal | 20 | Extra buffer when moving stops around the Vegas tunnel bands. |
| `TrailingStopPips` | decimal | 30 | Distance of the hard trailing stop when price leaves the tunnel. |
| `TrailingStep1Pips` | decimal | 55 | First profit layer derived from the EA's TP1 level. |
| `TrailingStep2Pips` | decimal | 89 | Second profit layer (TP2). |
| `TrailingStep3Pips` | decimal | 144 | Third profit layer (TP3) before switching to pure trailing. |
| `PartialClose1Volume` | decimal | 1 | Volume to close when `TrailingStep1Pips` is achieved. |
| `PartialClose2Volume` | decimal | 1 | Volume to close when `TrailingStep2Pips` is achieved. |
| `CrossEffectiveBars` | int | 2 | Number of completed candles while cross flags stay valid. |
| `TunnelBandWidthPips` | decimal | 5 | Neutral zone around the tunnel where new trades are avoided. |
| `TunnelSafeZonePips` | decimal | 120 | Maximum distance from the tunnel that still allows a breakout entry. |
| `EnableLondonSession` | bool | true | Enable trading between 07:00 and 16:00 exchange time. |
| `EnableNewYorkSession` | bool | true | Enable trading between 12:00 and 21:00 exchange time. |
| `EnableTokyoSession` | bool | false | Enable trading between 00:00–08:00 and on the 23:00 candle. |
| `FastPeriod` | int | 5 | Fast EMA length. |
| `SlowPeriod` | int | 12 | Slow EMA length. |
| `MaShift` | int | 0 | Horizontal displacement of the moving averages. |
| `MaMethod` | `MovingAverageType` | Exponential | EMA calculation mode (kept configurable for experimentation). |
| `MaAppliedPrice` | `AppliedPriceType` | Close | Candle price forwarded to the EMAs. |
| `RsiPeriod` | int | 21 | RSI averaging period. |
| `RsiAppliedPrice` | `AppliedPriceType` | Close | Candle price forwarded to the RSI. |
| `CandleType` | `DataType` | H1 time-frame | Candle series powering the strategy. |

## Implementation Notes

- The strategy runs entirely on the high-level candle subscription API and keeps indicator values in rolling lists to imitate the bar indexing (`Close[1]`, `Close[2]`) from the original script.
- Timers and tunnel flags reproduce the finite-state machine that determines whether a cross is still "fresh".
- `StartProtection()` is called on `OnStarted` so that StockSharp's built-in risk controls monitor the open position just like MetaTrader's hard stop-loss.
- Inline comments are written in English and describe each step of the conversion for easier maintenance.
