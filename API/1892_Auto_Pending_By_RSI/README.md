# Auto Pending By RSI

This strategy places pending limit orders after the Relative Strength Index (RSI) stays in extreme zones for several consecutive candles.

When the RSI remains below the oversold level for `MatchCount` candles, a buy limit order is registered below the candle close by `PendingOffset` price points. When the RSI stays above the overbought level for the same number of candles, a sell limit order is placed above the close by the same offset.

## Parameters
- `RsiPeriod` – RSI calculation period.
- `RsiOverbought` – level that defines the overbought zone.
- `RsiOversold` – level that defines the oversold zone.
- `PendingOffset` – distance from close price to place pending orders (price points).
- `MatchCount` – number of consecutive candles required before placing orders.
- `CandleType` – candle timeframe used for analysis.

Default values emulate the original MQL script and use 4‑hour candles.
