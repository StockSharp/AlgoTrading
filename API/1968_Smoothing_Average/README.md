# Smoothing Average Strategy

## Overview
The strategy trades around a simple moving average (SMA) with an additional smoothing offset. It attempts to exploit price deviations from the moving average by entering positions when the close price crosses an offset distance from the average.

## How It Works
- Calculate an SMA of the chosen candle type.
- If there is no open position:
  - Enter a short position when the close price is below `SMA + Smoothing`.
  - Enter a long position when the close price is above `SMA - Smoothing`.
- For an open short position:
  - Close the position when the close price rises above `SMA + Smoothing`.
- For an open long position:
  - Close the position when the close price falls below `SMA - Smoothing`.

The strategy uses market orders and works with finished candles only.

## Parameters
- **MA Period** – lookback period for the SMA.
- **Smoothing** – price offset added or subtracted from the SMA when generating signals.
- **Candle Type** – timeframe of candles used for calculations.

## Notes
This conversion is based on the original MQL4 script `smoothingaverage.mq4`.
