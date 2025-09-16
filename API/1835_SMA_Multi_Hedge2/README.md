# SMA Multi Hedge2 Strategy

This strategy trades a base security while hedging with a correlated instrument. Trend direction is determined by a Simple Moving Average (SMA). When the correlation between the base and hedge symbols exceeds a threshold, both instruments are traded to form a market-neutral pair.

## How It Works

1. Calculate the trend of the base symbol using an SMA of configurable length.
2. Measure correlation between the base and hedge symbols using the difference between price and its own SMA.
3. If the correlation reaches the expected level, open positions on both instruments. The hedge direction can follow or oppose the base depending on configuration.
4. Positions are closed automatically when the combined profit reaches the target value.

## Parameters

- `SmaPeriod` — period of the SMA used to detect trend. Default is 20.
- `CorrelationPeriod` — number of samples used to evaluate correlation. Default is 20.
- `ExpectedCorrelation` — minimum absolute correlation required to activate hedging. Default is 0.8.
- `ProfitTarget` — overall profit target in money units. Default is 30.
- `CandleType` — data type for candle subscription. Default is 1‑minute timeframe.
- `FollowBase` — if true, hedge trades in the same direction when correlation is positive.

## Indicators

- SMA
- Correlation (custom calculation)

## Notes

This is a simplified port of the original MQL strategy. Risk and money management should be adjusted before live trading.

