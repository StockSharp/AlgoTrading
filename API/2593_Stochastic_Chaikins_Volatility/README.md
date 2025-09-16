# Stochastic Chaikin's Volatility Strategy

## Overview
This strategy is a StockSharp port of the MetaTrader expert advisor `Exp_Stochastic_Chaikins_Volatility`. It analyses the spread between high and low prices, smooths that volatility with a configurable moving average, and then normalizes the result using a stochastic-like oscillator. Trading decisions follow the original counter-trend logic: the strategy looks for turning points in the oscillator to fade short-term extremes while optionally closing existing positions when momentum flips back.

## Indicator construction
1. **Chaikin-style volatility** – the difference between the candle high and low is smoothed with the *primary* moving average. Supported smoothing methods are:
   - Simple (SMA)
   - Exponential (EMA)
   - Smoothed/Wilder (SMMA)
   - Linear weighted (LWMA)
   - Jurik (JMA approximation)
2. **Stochastic normalization** – the most recent `Stochastic Length` smoothed values define the highest and lowest range. The current smoothed value is normalized into a 0–100 range using that window.
3. **Secondary smoothing** – a second moving average (method selectable from the same list) is applied to the normalized value to obtain the main oscillator line. Internally the signal line is simply the oscillator value from the previous completed candle, replicating the MQL indicator buffer behaviour.

## Trading logic
- **Entry**
  - *Buy*: when the main oscillator formed a lower high (previous value greater than its own prior value, current value crosses below that previous value). This mirrors the original EA's contrarian long trigger.
  - *Sell*: when the oscillator formed a higher low (previous value lower than its own prior value, current value crosses above that previous value).
- **Exit**
  - Long positions close when the previous oscillator value moves below its older value (downward momentum reappears).
  - Short positions close when the previous oscillator value rises above its older value.
- Signal evaluation uses the `Signal Shift` parameter to inspect completed candles. The defaults emulate the MQL setting of 1 bar.

## Parameters
| Name | Description |
| --- | --- |
| `Candle Type` | Timeframe used for all calculations (default 4-hour time candles). |
| `Primary Method` / `Primary Length` | Moving-average type and length for smoothing the high–low spread. |
| `Secondary Method` / `Secondary Length` | Moving-average type and length for smoothing the normalized oscillator. |
| `Stochastic Length` | Lookback window for the highest/lowest range used in the normalization step. |
| `Signal Shift` | Number of completed candles between the current bar and the bar used for signal evaluation. Must stay ≥1. |
| `Allow Long/Short Entry` | Enable or disable opening long or short trades. |
| `Allow Long/Short Exit` | Enable or disable position closing when the oscillator reverses. |
| `High/Middle/Low Level` | Visual guide levels reproduced from the original indicator (no direct trading effect). |

## Usage notes
- The StockSharp port keeps the original counter-trend behaviour but uses StockSharp moving averages. Exotic methods from the MQL library (ParMA, VIDYA, AMA, etc.) are mapped to the nearest available smoothing option; choose Jurik for a closer approximation when needed.
- Position sizing follows the base strategy `Volume` property. Stop-loss and take-profit management from the MQL helper library is not replicated; exits rely on oscillator reversals or external risk management such as `StartProtection`.
- Signals are calculated on finished candles only. Ensure that the data feed provides the selected `Candle Type` with sufficient history so that both smoothing stages and the stochastic window can warm up.
