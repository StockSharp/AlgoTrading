# Simple Multiple Time Frame Moving Average Strategy

This strategy replicates the logic of `simple_multiple_time_frame_moving_average.mq4`. It aligns trends across two time frames by using simple moving averages.

## Strategy Logic
- Calculate SMA with period `Length` on 1-hour and 4-hour candles.
- Enter long when both SMAs are rising.
- Enter short when both SMAs are falling.
- Close a long position when either SMA turns down.
- Close a short position when either SMA turns up.
- Only one position can be active at any time.

## Parameters
- **MA Length** (`Length`): period used for both moving averages.
- **Short Time Frame** (`ShortCandleType`): time frame for the first SMA (default 1 hour).
- **Long Time Frame** (`LongCandleType`): time frame for the second SMA (default 4 hours).

Trade volume is taken from the strategy's `Volume` property.

## Notes
This implementation focuses on the hourly and four-hour averages used in the original MQL version and omits unused higher time frame calculations.
