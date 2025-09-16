# SSB5_123 Multi-Indicator Strategy

## Overview
This strategy is a StockSharp port of the MetaTrader 5 expert advisor "ssb5_123". The original code comes from the SSB (Step by Step) collection by Yury V. Reshetov and combines several classic oscillators to confirm directional breakouts. The StockSharp version keeps the same logic while using the high-level candle subscription API and native indicator implementations.

The algorithm works exclusively on completed candles. It compares the opening price of the current bar with the previous bar, checks momentum and direction of the Awesome Oscillator, MACD, and OsMA histogram, and verifies that price is trading above or below a smoothed moving average. Additional confirmation is obtained from the stochastic oscillator by requiring both %K and %D to be above or below the 50 level.

## Indicators and Signals
The following indicators are employed exactly as in the MetaTrader version:

- **Smoothed Moving Average (SMMA)**: 45-period smoothed moving average calculated from the candle opens. The entry direction requires the open price to be on the correct side of the average.
- **MACD (fast 47, slow 95, signal 74)**: The main line must be positive for long trades (negative for short trades) and it must be rising (falling) compared with the previous candle.
- **OsMA Histogram**: Computed as MACD minus its signal line. The histogram must decrease for long trades and increase for short trades, mirroring the original `fosma1()` function.
- **Awesome Oscillator**: Uses the default 5/34 smoothed moving averages of the median price. The oscillator value must be positive for longs (negative for shorts) and its momentum between the last two bars must point in the trade direction.
- **Stochastic Oscillator (K=25, D=12, Slowing=56)**: Both %K and %D lines have to be above 50 for long trades and below 50 for short trades, providing a regime filter.

## Trading Logic
1. Wait for a new completed candle.
2. Evaluate the **long setup**. All of the following conditions must be true:
   - Current candle open is less than or equal to the previous candle open.
   - Awesome Oscillator is positive and falling versus the previous value.
   - MACD main line is positive and rising versus the previous value.
   - OsMA histogram is not increasing (current histogram minus previous histogram is less than or equal to zero).
   - Current candle open is above the smoothed moving average.
   - Stochastic %K and %D lines are at or above 50.
3. Evaluate the **short setup**. All of the following conditions must be true:
   - Current candle open is greater than or equal to the previous candle open.
   - Awesome Oscillator is negative and rising versus the previous value.
   - MACD main line is negative and falling versus the previous value.
   - OsMA histogram is not decreasing (current histogram minus previous histogram is greater than or equal to zero).
   - Current candle open is below the smoothed moving average.
   - Stochastic %K and %D lines are at or below 50.
4. If a position already exists, an opposite signal closes it immediately, replicating the original MetaTrader order management.
5. When flat, a long entry takes priority: if both signals happen to be true (possible when all indicators are exactly zero), the strategy opens a long position. Otherwise, it opens a short position when only the short conditions are satisfied.

## Parameters
- **SMMA Period** – length of the smoothed moving average filter (default 45).
- **MACD Fast / Slow / Signal** – EMA periods for the MACD indicator (47 / 95 / 74).
- **Stochastic %K / %D / Slowing** – main period, smoothing period, and additional slowing for the stochastic oscillator (25 / 12 / 56).
- **Order Volume** – quantity used for market orders (default 1).
- **Candle Type** – time frame of the input candles (default 1 hour). Adjust this to match the timeframe used in MetaTrader.

## Usage Notes
- The strategy operates on finished candles only; intrabar updates are ignored.
- Indicator values from the previous candle are cached so that the momentum comparisons match the exact behavior of the original `fao1`, `fmacd1`, and `fosma1` helper functions.
- There are no built-in stop-loss or take-profit rules in the original expert advisor. Risk management should be added externally if required.
- Default indicator settings match the provided MQL parameters, but all values are exposed as `StrategyParam` objects and can be optimized through the StockSharp optimizer.

## Conversion Considerations
- The MetaTrader version uses a magic number and manual volume validation; these parts are not needed in StockSharp and were omitted.
- Order closing logic follows the same precedence as the MQL script: positions are closed first, and new entries are only taken when the strategy is flat.
- The Awesome Oscillator and MACD implementations come from the StockSharp indicator library, removing the need for manual buffer handling present in the original code.
