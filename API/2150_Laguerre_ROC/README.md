# Laguerre ROC Strategy

This strategy uses the Laguerre rate-of-change oscillator to capture trend reversals.

The Laguerre ROC oscillator smooths rate of change through a four-stage Laguerre filter.
Values are normalized between 0 and 1. Two thresholds define overbought and oversold zones:

- **Up Level** – values above this level indicate strong upward momentum.
- **Down Level** – values below this level indicate strong downward momentum.

Trading logic:

1. When the oscillator falls from the overbought zone (previous value above Up Level
   and current value below) the strategy enters a long position.
2. When the oscillator rises from the oversold zone (previous value below Down Level
   and current value above) the strategy enters a short position.
3. If a long position is open and the oscillator turns bearish (previous value below the neutral
   level of 0.5) the position is closed.
4. If a short position is open and the oscillator turns bullish (previous value above 0.5)
   the position is closed.

Parameters:

- **Period** – lookback length for the rate-of-change calculation.
- **Gamma** – smoothing factor for the Laguerre filter.
- **Up Level** – overbought threshold.
- **Down Level** – oversold threshold.
- **Candle Type** – timeframe used for candle data.

The example demonstrates how custom indicator logic can be recreated within a high-level
StockSharp strategy using built-in rate-of-change and manual Laguerre filtering.
