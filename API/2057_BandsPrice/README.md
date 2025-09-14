# Bands Price Strategy

This strategy is a translation of the **i-BandsPrice** expert from MetaTrader. It uses Bollinger Bands to measure the relative position of price inside the channel and reacts when the value leaves extreme zones.

## Logic

1. Build Bollinger Bands with configurable period and deviation.
2. Calculate the price location inside the band as a value between -50 and +50.
3. Smooth the value with a simple moving average.
4. Generate a color code:
   - `4` when the smoothed value is above the upper level.
   - `0` when the smoothed value is below the lower level.
   - Other numbers represent intermediary zones.
5. A long position opens when the indicator leaves the upper zone (`4` → not `4`).
6. A short position opens when the indicator leaves the lower zone (`0` → positive).
7. Long positions close when the value becomes non-positive.
8. Short positions close when the value becomes non-negative.

## Parameters

| Name | Description |
|------|-------------|
| **BuyOpen** | Enable long entries. |
| **SellOpen** | Enable short entries. |
| **BuyClose** | Enable closing of long positions. |
| **SellClose** | Enable closing of short positions. |
| **BandsPeriod** | Period of the Bollinger Bands. |
| **BandsDeviation** | Deviation for the bands. |
| **Smooth** | Smoothing length for the internal value. |
| **UpLevel** | Upper threshold, default `25`. |
| **DnLevel** | Lower threshold, default `-25`. |
| **CandleType** | Candle timeframe used for calculations. |

## Notes

This strategy demonstrates how to migrate indicator-based logic from MetaTrader to StockSharp using high level API with `SubscribeCandles` and `Bind`.
