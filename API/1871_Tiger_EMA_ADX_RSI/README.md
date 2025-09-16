# Tiger EMA ADX RSI Strategy

This strategy follows the trend using a crossover of two exponential moving averages (EMAs) and filters trades with Average Directional Index (ADX) and Relative Strength Index (RSI). The fast EMA is compared with the slow EMA to determine trend direction. Trades are allowed only when ADX exceeds a configurable threshold and RSI stays within upper and lower bounds.

If no position is open and all conditions are satisfied, the strategy enters in the direction of the trend. Each entry sets fixed take-profit and stop-loss distances from the entry price. The position is closed when either level is reached. The order volume is defined by the strategy `Volume` property.

## Parameters

- **Fast EMA** – period of the fast exponential moving average.
- **Slow EMA** – period of the slow exponential moving average.
- **ADX Period** – period of ADX calculation.
- **ADX Threshold** – minimum ADX value required to trade.
- **RSI Period** – period of RSI calculation.
- **RSI Upper** – maximum RSI value for long entries.
- **RSI Lower** – minimum RSI value for short entries.
- **Take Profit** – distance from entry price to take profit in price points.
- **Stop Loss** – distance from entry price to stop loss in price points.
- **Candle Type** – timeframe or other candle type used for indicator calculations.
