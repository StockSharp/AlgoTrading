# Intraday Beta Strategy

This strategy searches for intraday turning points using smoothed moving average slopes and the Relative Strength Index (RSI).
A long position is opened when the slope of the 10-period moving average turns upward after a downward move, the RSI is below 70,
and the previous candle is bullish. A short position is opened when the slope turns downward after an upward move, the RSI is
above 30, and the previous candle is bearish.

An Average True Range (ATR) filter blocks new entries when volatility is too high. Open positions are protected by an adaptive
trailing stop that moves in the trade's favor and exits when price crosses the stop level.

## Parameters
- **RSI Period** – period of the RSI indicator.
- **Trailing Stop** – trailing stop distance in price units.
- **ATR Threshold** – maximum ATR value allowed for trading.
- **Candle Type** – timeframe of candles used for analysis.
