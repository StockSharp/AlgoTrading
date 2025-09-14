# XDPO Histogram Strategy

## Overview

The XDPO Histogram strategy adapts the original MQL5 expert *Exp_XDPO_Histogram*. It builds a double smoothed detrended price oscillator (XDPO) from closing prices. The oscillator is obtained by subtracting a moving average from price and smoothing this difference with a second moving average. Histogram dynamics provide signals for opening and closing trades.

## Trading Logic

- When the oscillator turns upward, all short positions are closed. If the current oscillator value exceeds the previous one, a new long position is opened.
- When the oscillator turns downward, all long positions are closed. If the current oscillator value is below the previous one, a new short position is opened.
- Calculations are performed only on completed candles.

## Parameters

- `FirstMaLength` – length of the first moving average applied to the price.
- `SecondMaLength` – length of the moving average applied to the difference between price and the first MA.
- `CandleType` – candle type used for all computations.

## Notes

- Moving averages are implemented with `SimpleMovingAverage` indicators.
- The strategy uses market orders (`BuyMarket` and `SellMarket`) and closes opposite positions before opening new ones.
