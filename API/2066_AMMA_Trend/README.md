# AMMA Trend Strategy

## Overview

The strategy uses the **Modified Moving Average (AMMA)** indicator to capture short term trend changes. It analyzes the direction of the AMMA slope on recent candles and opens a position in the direction of the emerging trend while closing the opposite one.

## How it works

1. A `ModifiedMovingAverage` with a configurable period is calculated on the selected timeframe.
2. On every finished candle, the strategy compares the last three AMMA values.
3. If the indicator values form a rising sequence and the most recent value is greater than the previous one, a long position is opened. Any short position is closed.
4. If the indicator values form a falling sequence and the most recent value is less than the previous one, a short position is opened. Any long position is closed.

## Parameters

- `CandleType` – timeframe of candles used for calculations.
- `MaPeriod` – period of the modified moving average.
- `AllowLongEntry` – enable opening long positions.
- `AllowShortEntry` – enable opening short positions.
- `AllowLongExit` – enable closing long positions.
- `AllowShortExit` – enable closing short positions.

## Notes

The strategy operates on completed candles only and relies on the built-in `BuyMarket` and `SellMarket` methods for order execution. Risk management functions can be added externally using the standard `Strategy` properties.
