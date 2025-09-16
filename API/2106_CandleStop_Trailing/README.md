# CandleStop Trailing Strategy

## Overview
This strategy implements trailing stop management based on the CandleStop approach. It analyzes completed candles and moves the stop level only in the direction of the trade. The algorithm relies on Donchian channels with separate look-back periods for long and short positions, making it suitable for attaching to manual trades or other entry strategies.

## Parameters
- **Up Trail Periods** – number of candles used to calculate the highest high for short position trailing.
- **Down Trail Periods** – number of candles used to calculate the lowest low for long position trailing.
- **Candle Type** – timeframe of candles used for analysis.

## Strategy Logic
1. Wait for an existing position. The strategy does not open trades on its own.
2. For long positions:
   - Calculate the lowest low over *Down Trail Periods*.
   - Move the stop to this value if it is higher than the previous stop.
   - If price touches or falls below the stop, exit the position with a market order.
3. For short positions:
   - Calculate the highest high over *Up Trail Periods*.
   - Move the stop to this value if it is lower than the previous stop.
   - If price touches or rises above the stop, buy back the position with a market order.

## Usage Notes
- Designed for use with StockSharp high-level API and candle subscriptions.
- Suitable for protecting positions opened manually or by other strategies.
- Chart output includes candles, channel lines and executed trades for visualization.
