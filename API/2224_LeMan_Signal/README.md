# LeMan Signal Strategy

## Overview

LeMan Signal Strategy is a port of the original MetaTrader LeManSignal expert advisor. The approach analyses recent highs and lows over two sequential periods to detect potential trend reversals. When specific patterns are found a long or short position is opened at the next candle.

## How It Works

1. The strategy observes completed candles of the selected timeframe.
2. For the previous bar it compares the highest highs and lowest lows in two consecutive ranges:
   - `H1` and `H2` are the maxima of two adjacent ranges.
   - `H3` and `H4` are the maxima of the next pair of ranges.
   - `L1` and `L2` are the minima of two adjacent ranges.
   - `L3` and `L4` are the minima of the next pair of ranges.
3. A **buy** signal is triggered if `H3 <= H4` and `H1 > H2`.
4. A **sell** signal is triggered if `L3 >= L4` and `L1 < L2`.
5. Orders are executed at market price. Any open opposite position is closed automatically.
6. Optional risk management is applied through `StartProtection` with default stop-loss and take-profit values of 1% and 2% respectively.

## Parameters

- **Period** – lookback length of the indicator.
- **Signal Bar** – offset used to confirm the signal (default 1).
- **Candle Type** – timeframe of the candles to analyse.

## Notes

- The strategy only reacts to finished candles.
- It does not maintain additional collections; internal buffers are limited to the minimum necessary for calculations.
- To use the strategy, add it to a StockSharp terminal, set the desired instrument and parameters, and start the strategy.

