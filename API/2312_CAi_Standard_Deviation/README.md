# CAi Standard Deviation Strategy

This strategy is a StockSharp port of the original MQL5 expert **Exp_i-CAi_StDev**. It combines a moving average with standard deviation bands to detect breakouts and subsequent reversals.

## Strategy Logic

1. Calculate a simple moving average (SMA) over the specified period.
2. Compute the standard deviation of closing prices over the same period.
3. Build two sets of bands around the SMA:
   - **Entry bands**: SMA ± `OpenMultiplier` × StdDev.
   - **Exit bands**: SMA ± `CloseMultiplier` × StdDev.
4. Open a long position when the price closes above the upper entry band.
5. Open a short position when the price closes below the lower entry band.
6. Close an existing long position when price drops below the upper exit band.
7. Close an existing short position when price rises above the lower exit band.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `MaLength` | Length of the moving average and standard deviation calculation | 12 |
| `StdDevPeriod` | Period for the standard deviation indicator | 9 |
| `OpenMultiplier` | Multiplier for entry bands | 2.5 |
| `CloseMultiplier` | Multiplier for exit bands | 1.5 |
| `CandleType` | Type of candles used by the strategy | 5-minute candles |

## Notes

- The strategy uses the high-level API with `Bind` to receive indicator values.
- Only finished candles are processed to avoid premature signals.
- All comments in the source code are provided in English.

