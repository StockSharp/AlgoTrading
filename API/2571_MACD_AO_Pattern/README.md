# MACD AO Pattern Strategy

## Overview
This strategy is a faithful StockSharp port of the FORTRADER `MACD.mq5` expert advisor. It implements the "AOP" pattern that watches the MACD oscillator for deep excursions away from the zero line followed by a hook back toward neutrality. When the hook is confirmed the strategy enters in the direction of the expected reversal and immediately applies fixed stop-loss and take-profit targets expressed in pips.

## Strategy Logic
### Data preparation
- Operates on the candle series selected by the `CandleType` parameter (5-minute candles by default).
- Uses a standard MACD indicator with configurable fast, slow and signal periods (defaults 12/26/9).
- Stores the MACD main line values of the three most recent completed candles in order to reproduce the MQL index-based access (`iMACD(...,1..3)`).

### Short setup (bearish hook)
1. **Arm** – once the MACD main line of the latest closed candle drops below `BearishExtremeLevel` (default −0.0015) the strategy starts watching for a reversal.
2. **Neutral pullback** – when MACD rises back above `BearishNeutralLevel` (default −0.0005) the hook validation stage becomes active.
3. **Hook confirmation** – the previous three MACD values must form a local maximum (`macd₁ < macd₂ > macd₃`) while the most recent value still stays below the neutral level and the older value remains above it. This recreates the original pattern that ensures momentum is fading.
4. **Entry** – if no long position is open (`Position <= 0`) a market sell order of `OrderVolume` is sent. Protective levels are calculated immediately: stop-loss above the entry by `StopLossPips` and take-profit below by `TakeProfitPips` (converted to price by `GetPipSize`).
5. Any positive MACD reading cancels the setup and resets the internal bearish state machine until a new deep negative stretch appears.

### Long setup (bullish hook)
1. **Arm** – once MACD rises above `BullishExtremeLevel` (default +0.0015) the bullish watch mode is activated.
2. **Immediate cancel** – if MACD falls below zero the bullish scenario is abandoned, mirroring the MQL logic.
3. **Neutral pullback** – a drop back below `BullishNeutralLevel` (default +0.0005) primes the hook confirmation.
4. **Hook confirmation** – the three stored MACD values must create a local minimum (`macd₁ > macd₂ < macd₃`) while respecting the neutral thresholds.
5. **Entry** – if there is no short exposure (`Position >= 0`) the strategy buys at market with `OrderVolume` and sets stop-loss and take-profit around the entry symmetric to the short rules.

### Risk management
- Stop-loss and take-profit are always active via `_stopPrice` and `_takePrice`. They are evaluated on every completed candle using the recorded high/low to emulate broker-side execution in the original EA.
- Pips are converted to absolute prices using `Security.PriceStep`. For 3- and 5-digit FX symbols the step is multiplied by 10 to match the MQL adjustment for fractional pips.
- Whenever the strategy exits a position because of the protective levels it clears them immediately and waits for a fresh setup on the next candles.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `CandleType` | Candle data series processed by the strategy. | 5-minute time frame |
| `OrderVolume` | Volume submitted with each market order. | 0.1 |
| `TakeProfitPips` | Distance to the profit target in pips. Marked for optimization. | 60 |
| `StopLossPips` | Distance to the stop-loss in pips. Marked for optimization. | 70 |
| `MacdFastPeriod` | Fast EMA length for MACD. | 12 |
| `MacdSlowPeriod` | Slow EMA length for MACD. | 26 |
| `MacdSignalPeriod` | Signal EMA length for MACD. | 9 |
| `BearishExtremeLevel` | Negative MACD threshold that arms short opportunities. | −0.0015 |
| `BearishNeutralLevel` | Negative MACD threshold used to validate the bearish hook. | −0.0005 |
| `BullishExtremeLevel` | Positive MACD threshold that arms long opportunities. | +0.0015 |
| `BullishNeutralLevel` | Positive MACD threshold used to validate the bullish hook. | +0.0005 |

## Additional Notes
- The strategy only reacts once per finished candle, mimicking the original `PrevBars` guard in MQL.
- Stop-loss/take-profit management is purely price-based; there are no trailing adjustments or re-entries until the full state machine cycles again.
- Designed for hedging accounts in the source EA, but this port enforces a single net position by checking `Position` before sending new orders.
- No Python version is provided as requested.
