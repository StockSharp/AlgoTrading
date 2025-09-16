# Robust EA Template Strategy

Strategy implementing the Robust EA Template from MQL.
It uses Commodity Channel Index (CCI) and Relative Strength Index (RSI) to generate entry signals and applies fixed take profit and stop loss.

## Logic
- Buy when CCI is in -200..-150 or -100..-50 and RSI is between 0 and 25.
- Sell when CCI is between 50 and 150 and RSI is between 80 and 100.
- Stop loss and take profit are defined in pips and converted to price points.

## Parameters
- `Candle Type` – candle data series.
- `CCI Period` – period of the CCI indicator.
- `RSI Period` – period of the RSI indicator.
- `Take Profit (pips)` – distance for profit target.
- `Stop Loss (pips)` – distance for stop loss.
- `Volume` – order volume.
