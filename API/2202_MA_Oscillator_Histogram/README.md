# MA Oscillator Histogram Strategy

## Overview
This strategy is a translation of the MQL5 expert **Exp_MAOscillatorHist.mq5**. It uses the difference between a fast and a slow Simple Moving Average (SMA) to form an oscillator. Trading signals are generated when the oscillator forms local minima or maxima, which are interpreted as potential trend reversals.

## Trading Logic
1. Two SMAs are calculated on the selected candle timeframe:
   - **Fast SMA** with a shorter period.
   - **Slow SMA** with a longer period.
2. The oscillator value is the fast SMA minus the slow SMA.
3. The strategy tracks the last three oscillator values. A local minimum occurs when the older value is higher than the previous one and the previous value is lower than the current one. A local maximum is the opposite.
4. When a local minimum is detected:
   - Close short positions (if allowed).
   - Open a new long position (if allowed).
5. When a local maximum is detected:
   - Close long positions (if allowed).
   - Open a new short position (if allowed).

## Parameters
| Parameter | Description |
|-----------|-------------|
| **Fast Period** | Period of the fast SMA. |
| **Slow Period** | Period of the slow SMA. |
| **Enable Buy Open** | If true, long positions can be opened. |
| **Enable Sell Open** | If true, short positions can be opened. |
| **Enable Buy Close** | If true, long positions can be closed on opposite signals. |
| **Enable Sell Close** | If true, short positions can be closed on opposite signals. |
| **Candle Type** | Timeframe of candles used for calculations. |

## Notes
- The strategy uses high-level StockSharp API with `SubscribeCandles` and indicator binding.
- `StartProtection` is enabled with market orders for safer execution.
- No Python version is provided.
