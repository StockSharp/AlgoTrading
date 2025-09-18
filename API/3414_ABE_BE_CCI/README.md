# ABE BE CCI Engulfing Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This StockSharp strategy ports the MetaTrader 5 expert advisor **Expert_ABE_BE_CCI** (folder `MQL/306`). The original EA combines Bullish/Bearish Engulfing candlestick patterns with a Commodity Channel Index (CCI) confirmation module and fixed-lot money management. The C# implementation keeps the same decision logic while leveraging the high-level subscription and indicator bindings provided by StockSharp.

The engine watches completed candles on the selected timeframe, computes a rolling average of candle bodies, an average of close prices, and a CCI with configurable period. Bullish or bearish engulfing patterns are only accepted when the candle bodies exceed the recent average and the midpoint of the engulfed candle is on the correct side of the moving average, mimicking the MQL `CCandlePattern` checks. Long trades require a bullish engulfing plus CCI below the oversold threshold, while short trades require the mirror condition with CCI above the overbought threshold. Position exits mirror the EA "vote" logic: CCI crossings of ±ExitLevel neutralize open positions regardless of direction.

## Workflow

1. Subscribe to the configured candle type and calculate:
   - Candle body average over `BodyAveragePeriod` bars.
   - Moving average of close prices over the same window.
   - Commodity Channel Index with length `CciPeriod`.
2. For every finished candle:
   - Verify the previous candle forms an opposite-colored engulfed bar.
   - Check that the engulfing body is larger than the rolling body average and closes beyond the previous open, replicating the MQL filters.
   - Confirm trend context by comparing the previous candle midpoint with the close-price moving average.
   - Confirm momentum with CCI vs. `EntryOversoldLevel` or `EntryOverboughtLevel`.
3. Manage trades:
   - If bullish conditions align and no long position is active, close shorts and buy the configured volume.
   - If bearish conditions align and no short is active, close longs and sell the configured volume.
   - Monitor CCI for exits: any crossing below `+ExitLevel` or across `-ExitLevel` closes longs, while crossings above `-ExitLevel` or below `+ExitLevel` close shorts, matching the EA's 40-point "vote" logic.

## Default Parameters

| Name | Default | Description |
| --- | --- | --- |
| `CciPeriod` | 49 | Length of the Commodity Channel Index indicator. |
| `BodyAveragePeriod` | 11 | Window for averaging candle body size and close-price mean. |
| `EntryOversoldLevel` | -50 | CCI threshold confirming bullish engulfing setups. |
| `EntryOverboughtLevel` | 50 | CCI threshold confirming bearish engulfing setups. |
| `ExitLevel` | 80 | Absolute CCI level that triggers position exits when crossed. |
| `CandleType` | 1 hour | Timeframe used for candle subscription. |

## Notes

- Volume handling mirrors typical StockSharp conversions: `Volume` defines base order size; opposite positions are flattened before reversing.
- Trailing and money management components (`TrailingNone`, `MoneyFixedLot`) from the MQL package are not recreated; StockSharp's order sizing already covers the fixed-lot behaviour.
- All comments inside the code are in English, tabs are used for indentation, and no indicator values are retrieved through `GetValue`, following the repository guidelines.
