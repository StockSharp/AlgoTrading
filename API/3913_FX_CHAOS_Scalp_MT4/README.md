# FX-CHAOS Scalp MT4 Strategy

## Overview
The FX-CHAOS Scalp MT4 strategy is a direct port of the MetaTrader 4 expert advisor that combines an Awesome Oscillator filter with ZigZag levels built on fractals. The StockSharp version keeps the multi-timeframe design of the original system: hourly candles generate trade signals while daily candles provide a higher-timeframe bias. Two embedded trackers reconstruct the "ZigZag on Fractals" indicator by scanning five-candle patterns and recording alternating swing highs and lows.

## Trading Workflow
1. **Data collection**
   - Hourly candles feed the primary execution logic and risk controls.
   - Daily candles update the long-term ZigZag swing used as a trend filter.
   - The Awesome Oscillator (5, 34) is evaluated on the hourly series through the high-level indicator API.
2. **ZigZag reconstruction**
   - Every finished candle is stored in a sliding five-element window.
   - When the middle candle forms an up fractal, the tracker saves the candle high as the latest swing and switches direction to "up"; a down fractal does the same for lows.
   - Consecutive swings in the same direction are only replaced if the new extreme is more pronounced, mimicking the buffer logic of the MT4 indicator.
3. **Signal detection**
   - The breakout buffer adds two price-step offsets to the previous hour high/low, mirroring the `2*Point` padding found in the original code.
   - For long entries the candle must open below the buffered high, close above it, remain below the most recent hourly ZigZag swing, close above the latest daily swing, and keep the Awesome Oscillator negative.
   - Short entries mirror the conditions using the buffered low, upper ZigZag level, and positive oscillator values.
4. **Order execution and conflict resolution**
   - Opposite positions are closed before a new order is sent so the strategy never keeps simultaneous long and short trades.
   - The executed close price is stored to derive stop-loss and take-profit distances in subsequent candles.

## Risk Management
- Stop-loss and take-profit thresholds are optional; a value of `0` disables the corresponding rule.
- At the end of each finished candle the strategy checks whether the candle range touched the configured stop or target and closes the position if the level was breached.
- When an opposite breakout appears the position is liquidated first, then the new trade is sent on the same candle to preserve the single-position rule.

## Parameters
| Name | Description |
| --- | --- |
| `Volume` | Trade volume in lots applied to every market order. |
| `Stop Loss (pts)` | Distance in points for the protective stop. Multiplied by the security price step. Set to `0` to disable. |
| `Take Profit (pts)` | Distance in points for the profit target. Multiplied by the price step. Set to `0` to disable. |
| `Breakout Buffer` | Additional points added to the previous candle extremum before testing breakouts. Default value reproduces the `2*Point` cushion used in MT4. |
| `Spread (pts)` | Average spread in points that is added to the breakout threshold on buy signals so the entry mirrors `2*Point + spread` from MT4. |
| `Trading Candle` | Primary timeframe used for entries (defaults to one hour). |
| `Daily Candle` | Higher timeframe used for the ZigZag filter (defaults to one day). |

## Implementation Notes
- The strategy relies on the high-level `SubscribeCandles` API and `BindEx` to avoid working with indicator buffers directly, respecting the repository guidelines.
- The price step retrieved from `Security.PriceStep` is used to convert parameter values expressed in points into absolute price distances. If the instrument lacks a step the code falls back to `1`.
- Both ZigZag trackers reset on `OnReseted` and pause trading until they accumulate enough candles to determine the first swing. This prevents premature entries when historical context is missing.
- Chart rendering draws the hourly candles, the Awesome Oscillator, and the strategy trades to help compare the StockSharp implementation with the MT4 template.
