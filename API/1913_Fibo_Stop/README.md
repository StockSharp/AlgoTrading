# Fibo Stop Strategy

## Overview
The Fibo Stop strategy trails the protective stop along Fibonacci retracement levels defined by two prices: start and end. The strategy opens a position in the direction from the start level to the end level and moves the stop to each new Fibonacci level once price crosses it.

## Algorithm
1. Determine direction from start to end price. If end is higher than start, a long position is opened; otherwise a short position.
2. Calculate Fibonacci levels: 0%, 23.6%, 38.6%, 50%, 61.8%, 78.6%, 100%, 127% based on the range.
3. Initial stop is placed behind the start level using the specified offset in price steps.
4. As the market price moves and crosses the next Fibonacci level, the stop is moved to that level minus/plus the offset.
5. Position is closed when price hits the trailing stop.

## Parameters
- `FiboStart` – base price where Fibonacci calculation begins.
- `FiboEnd` – final price defining the Fibonacci range.
- `OffsetPoints` – number of price steps added behind each Fibonacci level to place the stop.
- `CandleType` – candle series used for monitoring price.

## Notes
The strategy uses only completed candles and does not rely on indicator value history. It is intended as an example of managing a trailing stop with high-level StockSharp API.
