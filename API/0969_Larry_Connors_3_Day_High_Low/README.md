# Larry Connors 3 Day High Low Strategy

Implements the Larry Connors 3 Day High/Low mean reversion approach.

## Logic

- Buy when:
  - Close is above the long moving average.
  - Close is below the short moving average.
  - High and low have been lower for three consecutive candles.
- Exit when price closes above the short moving average.

## Parameters

- **Long MA Length** — period for the long SMA (default 200)
- **Short MA Length** — period for the short SMA (default 5)
- **Candle Type** — timeframe used for analysis
