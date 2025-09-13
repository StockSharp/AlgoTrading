# AMkA Signal Strategy

## Overview

This strategy uses the Kaufman Adaptive Moving Average (KAMA) derivative combined with a volatility filter based on standard deviation. A long position is opened when the rate of change of KAMA rises above a dynamic threshold; a short position is opened when it falls below the negative threshold. The threshold is calculated by multiplying the standard deviation of KAMA changes by a user-defined factor.

## Parameters

- **KAMA Length** – lookback period for the KAMA indicator.
- **Fast Period** – fast EMA period used in KAMA smoothing.
- **Slow Period** – slow EMA period used in KAMA smoothing.
- **Deviation Multiplier** – multiplier applied to the standard deviation to form the signal threshold.
- **Take Profit** – percentage for automatic profit fixing.
- **Stop Loss** – percentage for protective stop.
- **Candle Type** – timeframe of candles used for calculations.

## Trading Logic

1. Subscribe to candles of the selected timeframe.
2. Calculate KAMA for each candle and compute its change from the previous value.
3. Update the standard deviation indicator with the change values.
4. When the change exceeds `Deviation Multiplier * StdDev`, open or close positions:
   - If change is greater than the threshold: close short positions and open long.
   - If change is less than the negative threshold: close long positions and open short.
5. Protective orders for take profit and stop loss are managed automatically using `StartProtection`.

## Notes

The strategy works only with completed candles and uses tabs for indentation in the source code. All comments are written in English as required.
