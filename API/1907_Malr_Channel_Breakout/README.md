# MALR Channel Breakout Strategy

This strategy trades breakouts of a custom MALR (Moving Average Linear Regression) channel. The MALR indicator combines a simple moving average and a linear weighted moving average to form a central line. Standard deviation of price relative to this line creates two outer bands.

A long position is opened when the upper breakout band crosses below the closing price, indicating an upside breakout. A short position is opened when the lower breakout band crosses above the closing price, signalling a downside breakout.

## Parameters

- `MaPeriod` – period for the moving averages and standard deviation.
- `ChannelReversal` – width of the inner MALR channel measured in standard deviations.
- `ChannelBreakout` – additional width for the outer breakout channel.
- `CandleType` – type of candles used for calculations.

## How It Works

1. Calculate SMA and LWMA of the close price.
2. Compute the MALR line `FF = 3 * LWMA - 2 * SMA`.
3. Measure standard deviation of `close - FF` over the same period.
4. Derive breakout bands: `FF ± StdDev * (ChannelReversal + ChannelBreakout)`.
5. Enter long when the upper band crosses from above to below the close.
6. Enter short when the lower band crosses from below to above the close.

The strategy always closes the opposite position before opening a new one.

