# XMA Ichimoku Channel Strategy

## Overview

This strategy implements a channel breakout system based on the XMA Ichimoku concept. It builds a dynamic channel around a smoothed average of recent highs and lows and generates trades when price action confirms a breakout with a pullback.

## How It Works

1. **Highest and Lowest Values**: For each finished candle, the strategy calculates the highest high and lowest low over configurable lookback periods.
2. **Smoothed Midline**: The midpoint between the highest and lowest values is smoothed using a simple moving average.
3. **Channel Construction**: Upper and lower bands are derived from the smoothed midline by applying percentage offsets.
4. **Trading Logic**:
   - If the previous close was above the prior upper band and the current close returns below the current upper band, the strategy opens a long position and closes any existing short.
   - If the previous close was below the prior lower band and the current close returns above the current lower band, the strategy opens a short position and closes any existing long.

## Parameters

- **Up Period** – lookback period for the highest price.
- **Down Period** – lookback period for the lowest price.
- **MA Length** – length of the smoothing moving average.
- **Up Percent** – percentage added to the midline to form the upper band.
- **Down Percent** – percentage subtracted from the midline to form the lower band.
- **Candle Type** – timeframe of candles used for calculations.

## Usage Notes

- Trades are executed with market orders.
- Only finished candles are processed to avoid false signals.
- The strategy closes opposing positions before opening a new one.

## Disclaimer

This example is provided for educational purposes only. Test thoroughly before using in live trading.
