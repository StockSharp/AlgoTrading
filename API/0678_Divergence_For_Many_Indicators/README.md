# Divergence For Many Indicators Strategy

Detects bullish and bearish divergences between price and RSI and MACD histogram. When the number of divergences reaches the specified threshold, the strategy enters a trade in the opposite direction.

## Parameters
- `RsiPeriod` – period for RSI calculation.
- `MacdFastPeriod` – fast period for MACD.
- `MacdSlowPeriod` – slow period for MACD.
- `MacdSignalPeriod` – signal period for MACD.
- `MinDivergence` – minimum indicators confirming divergence.
- `CandleType` – candle type for subscription.
