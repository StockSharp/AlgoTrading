# MA Rounding Candle Strategy

## Overview

This strategy is an interpretation of the original MQL5 expert advisor "MA Rounding Candle". It uses two smoothed moving averages applied to candle open and close prices. The relative position of these averages defines the colour of a synthetic candle: green when the smoothed close is above the open, red when the close is below the open and gray when they are equal. A change in colour from the previous bar generates trade signals.

## Algorithm

1. For every completed candle the open and close values are smoothed with a simple moving average of configurable length.
2. The candle colour is defined by comparing the smoothed values:
   - **Up candle** – smoothed close is higher than smoothed open.
   - **Down candle** – smoothed close is lower than smoothed open.
   - **Neutral** – both values are equal.
3. If the previous candle was up and the current candle is not up, the strategy enters a long position and closes any short.
4. If the previous candle was down and the current candle is not down, the strategy enters a short position and closes any long.

## Parameters

- **MaLength** – period of the smoothing moving averages (default 12).
- **CandleType** – timeframe of the processed candles.

## Notes

The strategy demonstrates how to recreate signals from a custom indicator using only built‑in StockSharp tools. No stop loss or take profit is applied; positions are reversed immediately when the opposite signal appears.
