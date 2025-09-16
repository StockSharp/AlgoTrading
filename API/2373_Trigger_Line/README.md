# Trigger Line Strategy

The Trigger Line strategy combines a weighted trend line with a least squares moving average (LSMA). A long position is opened when the weighted trend line crosses above the LSMA, while a short position is opened when it crosses below.

## How It Works
- **Long Entry**: weighted trend line crosses above LSMA.
- **Long Exit**: weighted trend line crosses below LSMA.
- **Short Entry**: weighted trend line crosses below LSMA.
- **Short Exit**: weighted trend line crosses above LSMA.
- **Indicators**: Weighted Moving Average, Linear Regression (LSMA).

## Parameters
- **WT Period** – lookback for the weighted trend line.
- **LSMA Period** – smoothing period for LSMA.
- **Candle Type** – timeframe of candles used for calculations.
