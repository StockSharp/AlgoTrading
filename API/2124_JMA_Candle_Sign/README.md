# JMA Candle Sign Strategy

This strategy uses two Jurik Moving Averages (JMA) calculated on the open and close prices of each candle. A bullish signal occurs when the JMA of the open price crosses below the JMA of the close price, prompting a long entry. A bearish signal occurs when the JMA of the open price crosses above the JMA of the close price, prompting a short entry.

The default timeframe is four-hour candles with a JMA period of seven. Stop loss and take profit levels are defined in points and applied through built-in risk management. The strategy acts only on finished candles and maintains at most one open position.

## Parameters
- **JMA Length** – period for both JMAs.
- **Candle Type** – timeframe of processed candles.
- **Take Profit** – profit target in points.
- **Stop Loss** – maximum loss in points.
