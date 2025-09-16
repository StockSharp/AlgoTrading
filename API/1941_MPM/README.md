# MPM Momentum Strategy

This strategy is a simplified conversion of the original MQL expert `mpm-1_8.mq4`.
It waits for a sequence of progressive candles and then opens a position in the
same direction. Average True Range is used to evaluate candle size and to trail
stops.

## Parameters

| Name | Description |
| ---- | ----------- |
| `ProgressiveCandles` | Number of consecutive candles required to trigger a trade. |
| `ProgressiveSize` | Minimal candle body relative to ATR to count as progressive. |
| `StopRatio` | Ratio of ATR used to trail the stop level. |
| `AtrPeriod` | Period of the Average True Range indicator. |
| `CandleType` | Type of candles used by the strategy. |
| `ProfitPerLot` | Profit target per lot. |
| `BreakEvenPerLot` | Profit required to exit at breakeven. |
| `LossPerLot` | Maximum loss tolerated per lot. |

## Logic

1. On each finished candle the body size is compared with ATR.
2. A bullish or bearish counter is incremented when the body exceeds the
   `ProgressiveSize` threshold.
3. After `ProgressiveCandles` are seen in one direction a market order is sent.
4. The stop level is trailed by `StopRatio` of ATR.
5. Positions are closed when the stop is hit or when profit/loss targets are
   reached.
