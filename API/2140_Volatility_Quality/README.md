# Volatility Quality

A sample strategy demonstrating how to trade using the direction changes of a smoothed median price. The original MQL expert used the *Volatility Quality* indicator; this implementation approximates it with a simple moving average of the median price.

## Strategy Logic
- Calculate the median price of each candle `(High + Low) / 2`.
- Smooth the median price with a Simple Moving Average (SMA).
- Determine the indicator color: rising values are treated as **up** (color 0) and falling values as **down** (color 1).
- When the color switches from up to down, the strategy closes any short position and opens a long position.
- When the color switches from down to up, the strategy closes any long position and opens a short position.
- Basic risk management is applied via fixed stop loss and take profit levels.

## Parameters
| Name | Description |
|------|-------------|
| `Length` | Smoothing period for the SMA applied to median price. |
| `Candle Type` | Timeframe of candles used for calculations. |

## Disclaimer
This example is provided for educational purposes. It simplifies the original algorithm and may behave differently from the MQL version. Use at your own risk.
