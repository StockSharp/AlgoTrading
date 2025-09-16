# Color J2JMA StdDev Strategy

This strategy calculates the slope of a Jurik Moving Average (JMA) and compares it with the standard deviation of recent slopes. The idea is to capture strong directional moves when the slope exceeds a multiple of its recent volatility.

A new long position is opened when the JMA slope rises above the high threshold (K2 × standard deviation). A new short position is opened when the slope drops below the negative high threshold. Existing positions are closed when the slope crosses the opposite low threshold (K1 × standard deviation). Stop loss and take profit levels are applied in points from the entry price.

Parameters:
- **JMA Length** – period of the Jurik moving average.
- **StdDev Period** – number of recent slopes used for standard deviation.
- **K1** – multiplier for low threshold used to close positions.
- **K2** – multiplier for high threshold used to open positions.
- **Candle Type** – timeframe of candles for calculations.
- **Stop Loss** – protective stop in points.
- **Take Profit** – profit target in points.
