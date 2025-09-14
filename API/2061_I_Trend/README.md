# I Trend Strategy

## Overview

The **I Trend Strategy** is a trend-following trading algorithm converted from the original MQL5 expert `Exp_i_Trend`. It combines a moving average with Bollinger Bands to identify momentum shifts. The strategy calculates a custom *iTrend* value and a corresponding signal line and opens or closes positions when crossovers occur.

## How It Works

1. **Indicator Setup**
   - Calculates an Exponential Moving Average (EMA) with configurable period.
   - Builds Bollinger Bands using the same timeframe and deviation parameters.
   - Derives the *iTrend* value as the difference between the chosen price and the selected Bollinger Band line (upper, lower or middle).
   - Computes a signal line as `2 * MA - (High + Low)`.
2. **Signal Generation**
   - When the iTrend crosses **above** the signal line, the strategy closes short positions and opens a long position.
   - When the iTrend crosses **below** the signal line, the strategy closes long positions and opens a short position.
3. **Order Execution**
   - Entries and exits are executed at market price.
   - Position size is defined by the strategy parameter `Volume`.

## Parameters

| Name | Description |
|------|-------------|
| `MaPeriod` | Period of the moving average used in calculations. |
| `BbPeriod` | Period of the Bollinger Bands. |
| `BbDeviation` | Standard deviation for the Bollinger Bands. |
| `PriceType` | Price type used to compute the iTrend value (Close, Open, High, Low, Median, Typical, etc.). |
| `BbMode` | Selects which Bollinger Band line is used (Upper, Lower, Middle). |
| `CandleType` | Time frame of candles supplied to the strategy. |
| `Volume` | Order volume for entries. |

## Notes

- The strategy works on completed candles only; unfinished candles are ignored.
- It is designed for educational purposes and may require adjustments for live trading.
