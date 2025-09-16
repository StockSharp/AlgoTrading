# ColorXvaMA Digit StDev Strategy

## Overview
This strategy trades based on how far the price deviates from an exponential moving average (EMA). Two deviation multipliers (K1 and K2) define inner and outer bands calculated from the standard deviation of price.

When the price rises above the EMA by K2 standard deviations, the strategy enters a long position. When the price falls below the EMA by K2 standard deviations, it enters a short position. Existing positions are closed once the deviation returns inside the inner band defined by K1.

## Parameters
- **EMA Length** – period of the exponential moving average.
- **StdDev Length** – period for the standard deviation calculation.
- **Deviation K1** – multiplier for the inner band used to exit positions.
- **Deviation K2** – multiplier for the outer band used to open positions.
- **Candle Type** – timeframe of the candles.

## Indicators
- Exponential Moving Average
- StandardDeviation

## How It Works
1. Subscribe to candles of the chosen timeframe.
2. Calculate EMA and standard deviation of price.
3. Compute price deviation from the EMA.
4. Enter long/short when deviation exceeds ±K2×StdDev.
5. Exit when deviation returns within ±K1×StdDev.

This approach seeks to capture strong mean deviations and exit on reversion.
