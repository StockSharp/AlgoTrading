# Bago EA Strategy

The strategy replicates the MetaTrader "Bago EA" expert advisor. It trades trend-following breakouts confirmed by both moving-average and RSI crosses, while the Vegas tunnel (144/169 EMA pair) provides spatial filters and trailing anchors.

## Trading Logic

1. **Indicator preparation**
   - Two EMAs (periods `FastPeriod` and `SlowPeriod`, method `MaMethod`, price `MaAppliedPrice`).
   - Vegas tunnel EMAs (periods 144 and 169, same method/price) to detect the directional channel.
   - RSI (`RsiPeriod`, `RsiAppliedPrice`) for confirmation.
   - All price-to-pip conversions use the instrument `PriceStep` with 3/5-digit adjustment like the original EA.
2. **Cross state machine**
   - EMA cross up/down and RSI cross above/below 50 are tracked with timers. Each state remains active for `CrossEffectiveBars` candles and is reset by the opposite cross or the timeout.
   - Tunnel crosses mark when price moves from one side of the Vegas tunnel to the other.
3. **Entry conditions**
   - **Long**: both EMA and RSI cross up are active *and* price either
     - Closes above the tunnel by at least `TunnelBandWidthPips` but not further than `TunnelSafeZonePips`, with a bullish candle body, or
     - Closes below the tunnel by `TunnelBandWidthPips`, signalling a bounce from below.
   - **Short**: mirror logic with EMA/RSI crosses down.
   - Trading is allowed only inside enabled sessions (London 07–16, New York 12–21, Tokyo 00–08, or any bar closing after 23:00).
4. **Order handling**
   - New positions are opened with volume `TradeVolume`. Opposite positions are closed before reversing.
   - Initial stop is set at `StopLossPips` from the close price. Stop-to-tunnel offsets use `StopLossToFiboPips`.
5. **Trailing and partial exits**
   - As price advances beyond Vegas tunnel levels the stop moves:
     - Inside the tunnel, the stop parks at `tunnel ± (TrailingStepX + StopLossToFibo)`.
     - Outside the tunnel, a hard trailing of `TrailingStopPips` is applied behind price.
   - Partial exits close `PartialClose1Volume` at `TrailingStep1Pips` and `PartialClose2Volume` at `TrailingStep2Pips` once price travelled far enough from the entry.
   - An opposite EMA/RSI cross closes the whole position immediately.
6. **Stops**
   - Protective orders are maintained as market-stop orders. They are cancelled whenever the position is closed.

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `TradeVolume` | decimal | 3 | Order size in lots. |
| `StopLossPips` | decimal | 30 | Initial stop-loss distance. |
| `StopLossToFiboPips` | decimal | 20 | Extra buffer when parking stops around the Vegas tunnel. |
| `TrailingStopPips` | decimal | 30 | Distance of the trailing stop once price leaves the tunnel. |
| `TrailingStep1Pips` | decimal | 55 | First profit layer for partial exit and stop relocation. |
| `TrailingStep2Pips` | decimal | 89 | Second profit layer for partial exit and trailing. |
| `TrailingStep3Pips` | decimal | 144 | Final layer before pure trailing is used. |
| `PartialClose1Volume` | decimal | 1 | Volume closed at `TrailingStep1Pips`. |
| `PartialClose2Volume` | decimal | 1 | Volume closed at `TrailingStep2Pips`. |
| `CrossEffectiveBars` | int | 2 | Number of bars for which EMA/RSI crosses remain valid. |
| `TunnelBandWidthPips` | decimal | 5 | Neutral zone around the Vegas tunnel where no trades are taken. |
| `TunnelSafeZonePips` | decimal | 120 | Maximum distance above the tunnel for long entries (and below for shorts). |
| `EnableLondonSession` | bool | true | Allow signals during London hours. |
| `EnableNewYorkSession` | bool | true | Allow signals during New York hours. |
| `EnableTokyoSession` | bool | false | Allow signals during Tokyo hours. |
| `FastPeriod` | int | 5 | Fast EMA length. |
| `SlowPeriod` | int | 12 | Slow EMA length. |
| `MaShift` | int | 0 | Horizontal shift applied to all EMAs. |
| `MaMethod` | `MovingAverageType` | Exponential | Moving-average smoothing mode. |
| `MaAppliedPrice` | `AppliedPriceType` | Close | Candle price forwarded to the EMAs. |
| `RsiPeriod` | int | 21 | RSI averaging length. |
| `RsiAppliedPrice` | `AppliedPriceType` | Close | Candle price forwarded to the RSI. |
| `CandleType` | `DataType` | H1 time-frame | Candle series used for the calculation. |

## Notes

- The strategy keeps indicator states even outside the trading hours, exactly as in the original EA.
- Stop orders are managed via high-level API (`SellStop`/`BuyStop`) to mimic MetaTrader `PositionModify` calls.
- All comments and structure follow the repository guidelines (tabs for indentation, English inline comments).
