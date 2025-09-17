# ADX MACD Deev Strategy

## Overview
The **ADX MACD Deev Strategy** is a StockSharp port of the MetaTrader expert advisor with the same name. It combines the trend strength signal from the Average Directional Index (ADX) with momentum confirmation from the Moving Average Convergence Divergence (MACD). The strategy trades only when both indicators agree on the market direction and can optionally secure profits through trailing stops and partial position exits.

## How it Works
1. **Indicator preparation**
   - ADX is calculated with a configurable averaging period. The strategy tracks the latest ADX values and requires them to move consistently in one direction before allowing a trade.
   - MACD uses configurable fast, slow and signal exponential moving averages. The histogram and signal line must jointly show sustained growth for longs or sustained decline for shorts.
2. **Entry logic**
   - **Long entries**: triggered when the MACD histogram exceeds the `MACD Minimum (pips)` threshold, both MACD histogram and signal line increase for the selected number of bars, and ADX stays above the required strength while also rising.
   - **Short entries**: triggered when the MACD histogram is below the negative threshold, both MACD histogram and signal line decline over the selected interval, and ADX remains above the minimum while decreasing.
   - Only one position can be open at a time.
3. **Risk management**
   - Initial stop-loss and take-profit levels are placed in price units derived from the instrument `PriceStep` and the chosen pip distances.
   - A trailing stop can follow profitable positions once price advances by `Trailing Stop + Trailing Step` pips.
   - When `Take Half Profit` is enabled the strategy closes half of the current position at the take-profit level and lets the rest run with the trailing stop.

## Parameters
| Group | Name | Description |
| --- | --- | --- |
| Trading | Order Volume | Volume of each new market order. |
| Risk | Stop Loss (pips) | Initial stop-loss offset from entry. |
| Risk | Take Profit (pips) | Initial take-profit offset from entry. |
| Risk | Trailing Stop (pips) | Trailing stop distance. Set to zero to disable trailing. |
| Risk | Trailing Step (pips) | Extra price move before the trailing stop moves again. |
| Risk | Take Half Profit | Enables partial exit when the take-profit level is hit. |
| Indicators | ADX Period | ADX averaging period. |
| Indicators | ADX Bars Interval | Number of recent ADX bars that must trend in one direction. |
| Indicators | ADX Minimum | Minimum ADX value required for entries. |
| Indicators | MACD Fast EMA | Fast EMA length used by MACD. |
| Indicators | MACD Slow EMA | Slow EMA length used by MACD. |
| Indicators | MACD Signal EMA | Signal EMA length used by MACD. |
| Indicators | MACD Bars Interval | Number of MACD bars that must align in the same direction. |
| Indicators | MACD Minimum (pips) | Minimum MACD magnitude converted to pips. |
| General | Candle Type | Candle type or time-frame used for calculations. |

## Usage Notes
- The strategy requires instruments with a valid `PriceStep`. If `PriceStep` is zero the pip-based thresholds fall back to raw MACD values.
- Volume rounding for partial exits follows the `VolumeStep` of the instrument.
- Trailing stop adjustments are evaluated on closed candles only.
- The strategy uses high-level API bindings (`SubscribeCandles().BindEx(...)`) and does not rely on manual indicator value polling.
