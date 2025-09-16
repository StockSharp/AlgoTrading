# CSPA 1.43 Strategy

This strategy is an adaptation of the **CSPA-1_43** MQL expert advisor. It measures the strength of a currency pair using the Relative Strength Index (RSI). When the pair becomes sufficiently strong or weak, the strategy opens a position in the direction of the prevailing momentum and closes it when momentum fades.

## Logic

- Subscribe to candles of the selected security.
- Calculate the RSI value for each finished candle.
- Open a long position when RSI rises above the upper threshold.
- Open a short position when RSI falls below the lower threshold.
- Close the current position when RSI returns to the neutral zone.

## Parameters

| Name | Description | Default |
|------|-------------|---------|
| `StrengthPeriod` | Period used for the RSI indicator. | `14` |
| `Threshold` | Distance from the neutral RSI level of 50 used to generate signals. | `10` |
| `CandleType` | Time frame of the candles. | `1 hour` |

## Notes

- The strategy uses the high-level API with automatic indicator binding.
- Orders are executed using market orders (`BuyMarket` and `SellMarket`).
- Only finished candles are processed.

