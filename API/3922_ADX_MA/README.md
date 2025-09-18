# ADX & MA Strategy

## Overview

This strategy is a StockSharp port of the MetaTrader expert **ADX_MA (fortrader)**.
It combines a smoothed moving average (SMMA) filter with the Average Directional Index (ADX)
so that trades are taken only when the trend is both confirmed by a crossover and strong enough
according to ADX. The port keeps the asymmetric risk management from the original robot:
long positions use wide take-profit and trailing distances, while short trades employ tighter
targets and protection.

## Trading Logic

1. Build a smoothed moving average on median candle prices and an ADX with the configured periods.
2. Evaluate signals on closed candles only to mimic the MQL4 logic (`iClose(...,1)` / `iClose(...,2)`).
3. Enter long when the previous candle closes above the SMMA, the candle before it closes below the
   same SMMA value, and the previous ADX reading is above the threshold.
4. Enter short when the previous candle closes below the SMMA, the candle before it closes above the
   same SMMA value, and ADX is above the threshold.
5. Once in position, exits are driven by:
   - Moving-average flip in the opposite direction.
   - Individual stop-loss and take-profit levels measured in pips.
   - Optional trailing stop distances that ratchet in the trade's favor.

All price offsets are converted from pips using the security's price step. If the instrument does not
report a valid step, a value of 1 is used as a safe fallback.

## Parameters

| Name | Description |
| ---- | ----------- |
| `SMMA Period` | Length of the smoothed moving average (default 21). |
| `ADX Period` | Length of the Average Directional Index (default 14). |
| `ADX Threshold` | Minimum ADX value required to allow entries (default 16). |
| `Long Take Profit (pips)` | Take-profit distance for buy positions (default 1300 pips). |
| `Long Stop Loss (pips)` | Stop-loss distance for buy positions (default 30 pips). |
| `Long Trailing Stop (pips)` | Trailing-stop distance for buy positions (default 270 pips). |
| `Short Take Profit (pips)` | Take-profit distance for sell positions (default 160 pips). |
| `Short Stop Loss (pips)` | Stop-loss distance for sell positions (default 50 pips). |
| `Short Trailing Stop (pips)` | Trailing-stop distance for sell positions (default 20 pips). |
| `Volume` | Order volume used for new entries (default 0.1). |
| `Candle Type` | Primary candle series for calculations (default 1-minute time frame). |

All parameters are exposed for optimization. The defaults match the original EA settings.

## Notes

- Trailing stops activate only after the price moves at least the configured distance from the entry.
- Opposite signals close the active position before opening a new one.
- The strategy automatically draws candles, indicators, and own trades on the chart if a chart area is available.
- There are no automated tests for this port; use manual backtesting to validate the behaviour on your instruments.
