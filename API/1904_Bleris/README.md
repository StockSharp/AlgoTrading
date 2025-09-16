# Bleris Strategy

## Overview
The Bleris strategy analyzes the trend of recent price extremes to open trades in the direction of the prevailing trend.
The price series is split into three segments of length `SignalBarSample` and the highest highs and lowest lows of these segments are compared.

- **Indicators**: Highest, Lowest
- **Parameters**:
  - `SignalBarSample` – number of candles per segment.
  - `CounterTrend` – reverse the trading direction.
  - `Lots` – order volume.
  - `CandleType` – timeframe of the candles.
  - `AnotherOrderPips` – minimum distance in pips before another order of the same type is opened.

## How it Works
1. Highest and Lowest indicators calculate extreme prices over the recent `SignalBarSample` candles.
2. Decreasing highs signal a downtrend, increasing lows signal an uptrend.
3. The strategy buys on an uptrend and sells on a downtrend. With `CounterTrend` enabled the logic is inverted.
4. New orders of the same direction are ignored if the last order price is within `AnotherOrderPips`.

This example uses the high-level StockSharp API and is intended for educational purposes.
