# Roboti ADX Profit Strategy

This strategy converts the original **RobotiADXProfitwining.mq4** expert advisor into the StockSharp API. It relies on the Directional Movement Index (DMI) to determine trend direction.

## Trading Logic

- Uses the `DirectionalIndex` indicator with a default period of 14.
- Works on one-hour candles by default, but the timeframe can be changed.
- Enters a **long** position when the `+DI` line crosses above the `-DI` line and no long position is open.
- Enters a **short** position when the `-DI` line crosses above the `+DI` line and no short position is open.
- Positions are protected by a trailing stop expressed as a percentage of price.

## Parameters

| Name | Description | Default |
| ---- | ----------- | ------- |
| `DmiPeriod` | Period for DMI calculation. | 14 |
| `CandleType` | Candle type and timeframe used by the strategy. | 1 hour |
| `TrailingStopPercent` | Size of the trailing stop in percent. | 1% |

## Notes

The strategy uses the high-level binding API of StockSharp and avoids direct calls to indicator buffers. All comments in the code are in English as requested.
