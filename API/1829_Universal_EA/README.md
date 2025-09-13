# Universal EA Strategy

Strategy translated from MQL4 "Universal_EA".

This algorithm uses the Stochastic Oscillator to determine entry points.
A long position is opened when the %K line crosses above the %D line while
both are below the oversold threshold. A short position is opened when %K
crosses below %D and both are above the overbought threshold. Signals are
checked only on finished candles and positions are opened by market orders.

## Parameters
- **%K Period** – base period used to calculate %K.
- **%D Period** – smoothing period for the %D line.
- **Slowing** – additional smoothing applied to %K.
- **Oversold** – level below which the market is considered oversold.
- **Overbought** – level above which the market is considered overbought.
- **Candle Type** – candle timeframe or type used for analysis.
