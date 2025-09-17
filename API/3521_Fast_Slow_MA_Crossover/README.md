# Fast Slow MA Crossover Strategy

## Overview

The **Fast Slow MA Crossover Strategy** reproduces the behaviour of the original MetaTrader 4 expert advisor `_HPCS_FastSlowMACrosssover_MT4_EA_V01_WE`. The strategy watches two exponential moving averages (EMAs) calculated on the selected candle series and issues trades when the fast average crosses the slow one inside a configurable intraday trading window. Protective take-profit and stop-loss exits are expressed in pips so the behaviour matches the MQL implementation that relies on broker digits for price scaling.

## Trading Logic

1. Subscribe to the configured candle type (default: 1-minute candles).
2. Calculate two EMAs:
   - Fast EMA period (default **14**).
   - Slow EMA period (default **21**).
3. Evaluate each finished candle:
   - Check that the candle close time falls inside the allowed trading window.
   - Detect a **bullish crossover** when the fast EMA crosses above the slow EMA.
   - Detect a **bearish crossover** when the fast EMA crosses below the slow EMA.
4. Execute orders:
   - Close the opposite exposure if an inverse position is open.
   - Enter a market order with the configured volume (**Trade Volume** parameter).
   - Store the candle close price as the entry anchor for risk calculations.
5. Manage open positions using candle highs and lows:
   - Close a long position if the price moves **Stop Loss (pips)** below the entry.
   - Close a long position if the price rallies **Take Profit (pips)** above the entry.
   - Apply the symmetrical logic for short positions (stop above entry, target below entry).

## Parameters

| Parameter | Description |
|-----------|-------------|
| **Fast MA Period** | Length of the fast EMA used for crossover detection. |
| **Slow MA Period** | Length of the slow EMA. |
| **Take Profit (pips)** | Distance, in pips, used to compute the long and short profit targets. |
| **Stop Loss (pips)** | Distance, in pips, used to compute the protective stop prices. |
| **Start Time** | Beginning of the daily trading window (inclusive). |
| **Stop Time** | End of the daily trading window (inclusive). |
| **Candle Type** | Candle series used to feed the indicators. |
| **Trade Volume** | Market order volume for each signal. |

## Notes

- Pip size is derived from the security price step and decimal precision. When the instrument uses 5 or 3 decimal digits, the strategy multiplies the price step by **10** to match the MetaTrader pip calculation.
- The time filter supports overnight sessions. When **Start Time** is later than **Stop Time**, trading remains active until midnight and resumes from midnight to the stop time.
- Only one signal per candle is allowed, ensuring the behaviour matches the original EA that guarded against multiple submissions per bar.
- Protective exit orders are executed by the strategy logic instead of resting orders. This mirrors the EA approach where the stop loss and take profit levels were defined at order submission.
