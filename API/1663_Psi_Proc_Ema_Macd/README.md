# Psi Proc EMA MACD Strategy

This strategy replicates the T4 system from the original MQL expert `e-PSI@PROC.mq4`. It trades based on the alignment of multiple exponential moving averages and a MACD filter.

## Strategy Logic

1. Calculate EMA(200), EMA(50) and EMA(10) on each incoming candle.
2. Calculate MACD with parameters 12, 26, 9.
3. Go long when:
   - EMA200 is rising and EMA50 > EMA200.
   - EMA50 is rising and EMA10 > EMA50.
   - MACD is rising and above `LimitMACD`.
4. Go short when:
   - EMA200 is falling and EMA50 < EMA200.
   - EMA50 is falling and EMA10 < EMA50.
   - MACD is falling and below `-LimitMACD`.
5. Exit long when the price closes below EMA50.
6. Exit short when the price closes above EMA50.

Optional take-profit and trailing-stop protections are supported.

## Parameters

| Name | Description |
| ---- | ----------- |
| `LimitMACD` | Minimal absolute MACD level to allow entry. |
| `TakeProfitPoints` | Take-profit level in price points. |
| `TrailStopPoints` | Trailing stop level in price points. |
| `CandleType` | Timeframe of candles used by the strategy. |

## Notes

- Trades are opened with market orders.
- Only completed candles are processed.
- The strategy operates on a single security.

