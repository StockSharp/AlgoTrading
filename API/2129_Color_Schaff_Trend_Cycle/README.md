# Color Schaff Trend Cycle Strategy

This strategy trades based on the **Schaff Trend Cycle (STC)** indicator. STC applies a double stochastic calculation to a MACD series and oscillates between -100 and 100. Values above the high level suggest bullish pressure, while values below the low level suggest bearish pressure.

## Trading Logic

- Subscribe to candles of the selected timeframe.
- Compute MACD using fast and slow exponential averages.
- Apply two consecutive stochastic calculations to derive STC.
- When STC rises above the high level and continues upward:
  - Close any short position.
  - Enter a long position.
- When STC falls below the low level and continues downward:
  - Close any long position.
  - Enter a short position.

The strategy always acts on fully formed candles.

## Parameters

| Name | Description | Default |
|------|-------------|---------|
| `FastPeriod` | Fast EMA period used in MACD | `23` |
| `SlowPeriod` | Slow EMA period used in MACD | `50` |
| `Cycle` | Stochastic cycle length | `10` |
| `HighLevel` | Overbought threshold for STC | `60` |
| `LowLevel` | Oversold threshold for STC | `-60` |
| `CandleType` | Timeframe of processed candles | `4h` |

## Notes

- STC values are rescaled to a range of -100…100 for easier comparison with the default levels.
- Orders are sent with `BuyMarket()` and `SellMarket()` calls; positions are reversed automatically when opposite signals appear.
- This strategy focuses solely on the indicator signals and does not use stop-loss or take-profit orders.
