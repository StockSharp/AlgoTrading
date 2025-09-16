# JS Chaos Strategy

## Overview
The JS Chaos strategy replicates the behaviour of the original MetaTrader expert advisor "JS-Chaos" using the StockSharp high-level API. The strategy builds break-out entries around Bill Williams' Alligator structure and fractal levels, combines Awesome Oscillator and Acceleration/Deceleration confirmation, and manages open exposure with trailing stops, breakeven logic, and a rich time filter.

## Core Logic
1. **Indicator stack**
   - Bill Williams Alligator (Smoothed Moving Averages with 13/8/5 periods and 8/5/3 bar shifts) sampled on the median price.
   - Awesome Oscillator and a five-period SMA of AO to derive the Acceleration/Deceleration oscillator.
   - 21-period smoothed moving average for the trailing stop engine.
   - 10-period standard deviation used as an additional trailing condition.
   - Fractal detection over the last five highs/lows, storing the most recent formations for ten bars.
2. **Signal generation**
   - Bullish context requires `AO[0] > AO[1] > 0` and `Lips > Teeth > Jaw`.
   - Bearish context requires `AO[0] < AO[1] < 0` and `Lips < Teeth < Jaw`.
3. **Order placement**
   - When conditions align and the current time is tradable, the strategy queues two stop-style entries per direction: a primary order (2× base volume) and a secondary order (1× base volume). Both trigger at the latest qualifying fractal that stands beyond the Alligator lips.
   - Primary take-profit uses `Lips ± (Fractal − Lips) * Fibo1`. Secondary take-profit uses the `Fibo2` multiplier.
4. **Trade management**
   - Optional early exit when the lips cross above (for longs) or below (for shorts) the previous candle open.
   - Trailing stop pulls the protective level to the 21-period SMMA whenever standard deviation, AO and AC all advance in the trade direction.
   - Breakeven logic shifts the secondary trade stop once the primary trade has been completed and price has travelled by the configured extra pips.
   - Manual monitoring of stop-loss and take-profit levels closes trades via market orders when the corresponding price boundaries are breached.
5. **Time filter**
   - Trading window defined by start/end hours (with wrap-around support) and optional seasonal filters: disabled before Monday 03:00, after Friday 18:00, during the first nine days of January, and after 20 December. Setting `Use Time` to false turns the filter off entirely.

## Parameters
| Name | Description |
| ---- | ----------- |
| `UseTime` | Enables the time filter. |
| `OpenHour` / `CloseHour` | Hour boundaries for trading (0-23). |
| `BaseVolume` | Base order volume, used to size the two staged entries (2× for the primary, 1× for the secondary). |
| `IndentingPips` | Offset added/subtracted from fractal levels before placing stop orders (expressed in pips). |
| `Fibo1` / `Fibo2` | Fibonacci-style multipliers applied to the distance between the lips and the fractal for the take-profit targets. |
| `UseClosePositions` | Closes opposite positions when the lips cross the previous candle open. |
| `UseTrailing` | Enables the MA/oscillator-based trailing stop. |
| `UseBreakeven` | Activates breakeven management for the secondary position. |
| `BreakevenPlusPips` | Extra pips added on top of the entry price when moving the stop to breakeven. |
| `CandleType` | Time-frame of the candles processed by the strategy. |

## Notes
- The conversion keeps the staged order structure and management logic of the original MQL5 robot while taking advantage of StockSharp's candle subscription workflow.
- All calculations rely on finished candles; intrabar tick logic from the original EA is mirrored through market orders once the price range confirms a breakout.
- Pip conversion automatically adapts to instruments quoting with three or five decimal places (forex-like symbols).
