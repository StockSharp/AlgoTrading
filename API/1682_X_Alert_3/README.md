# X Alert 3 Strategy

This strategy replicates the logic of the original **X_alert_3.mq4** expert. It monitors two moving averages with configurable parameters and produces an informational alert when a crossover occurs.

## How it works

1. Two moving averages are calculated on every finished candle.
2. A bullish alert is generated when:
   - MA1 is above MA2 on the current candle.
   - MA1 is above MA2 on the previous candle.
   - MA1 was below MA2 two candles ago.
3. A bearish alert is generated when:
   - MA1 is below MA2 on the current candle.
   - MA1 is below MA2 on the previous candle.
   - MA1 was above MA2 two candles ago.
4. The strategy does **not** open or close any positions. It only writes messages to the log.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `Ma1Period` | Period of the first moving average. | `1` |
| `Ma1Type` | Type of the first moving average (Simple, Exponential, Smoothed, Weighted). | `Simple` |
| `Ma2Period` | Period of the second moving average. | `14` |
| `Ma2Type` | Type of the second moving average. | `Simple` |
| `PriceType` | Source price used in calculations (Close, Open, High, Low, Median, Typical, Weighted). | `Median` |
| `CandleType` | Candle series used for processing. | `1-minute` time frame |

## Notes

- The implementation keeps track of the last two differences between the moving averages to detect crossovers without accessing historical indicator values directly.
- Alerts are written using `AddInfoLog` to keep the strategy side-effect free.
- The original MetaTrader parameter `RunIntervalSeconds` is not required in StockSharp and has been omitted.

