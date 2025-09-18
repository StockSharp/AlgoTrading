# Hammer & Hanging Man with CCI Confirmation
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy reimplements the MetaTrader "AH HM CCI" expert in StockSharp. It watches for hammer and hanging man candlestick
patterns and requires confirmation from the Commodity Channel Index (CCI) before entering a trade. The extra confirmation filters
out weak patterns and helps align entries with the momentum shift signaled by CCI.

The logic runs on completed candles only and uses a short simple moving average (SMA) to define the prevailing trend. The previous
candle must be a hammer in a downtrend with oversold CCI to buy, or a hanging man in an uptrend with overbought CCI to sell. Exits
are managed when CCI crosses configurable trigger levels, replicating the vote-based exit logic from the original expert.

## Trading Logic

1. **Trend filter** – The midpoint of the previous candle has to be below (for longs) or above (for shorts) an SMA calculated on
   closing prices. This mimics the original wizard's moving-average trend check.
2. **Pattern detection** – The strategy evaluates the previous bar and checks:
   - Body entirely in the top third of the candle range.
   - Gap between the previous candle's open/close and the candle before it.
   - Directional context (hammer for a downtrend, hanging man for an uptrend).
3. **CCI confirmation** – The previous bar's CCI must be below the long threshold or above the short threshold. The default values
   match the MetaTrader template (40 for longs and 60 for shorts).
4. **Position exits** – Existing positions are closed when CCI crosses either the lower or upper exit thresholds. Crossing from
   below closes longs; crossing from above closes shorts.

## Parameters

| Name | Description | Default |
| ---- | ----------- | ------- |
| `CandleType` | Candle type and timeframe used for pattern recognition. | `TimeSpan.FromMinutes(15)` |
| `CciPeriod` | Number of bars used by the Commodity Channel Index. | `11` |
| `MaPeriod` | Number of bars in the SMA trend filter. | `5` |
| `LongConfirmationThreshold` | Maximum CCI value allowed for a hammer signal. | `40` |
| `ShortConfirmationThreshold` | Minimum CCI value allowed for a hanging man signal. | `60` |
| `ExitUpperThreshold` | CCI level that triggers exits after an upward crossing. | `70` |
| `ExitLowerThreshold` | Secondary exit level for early signals. | `30` |

All parameters are available for optimization. The thresholds accept negative values, so you can adapt the strategy to other
markets or noise levels by tightening or loosening the filters.

## Order Management

- **Entries** use market orders sized as `Volume + |Position|`, ensuring reversals are executed in a single trade.
- **Exits** rely purely on the CCI crosses to stay close to the MetaTrader expert. Add `StartProtection` calls if you need
  explicit stop-loss or take-profit levels.

## Usage Tips

- Apply the strategy on liquid instruments where candlestick gaps and tails are informative.
- Experiment with longer `CciPeriod` and `MaPeriod` values to smooth out noise when trading higher timeframes.
- Lowering `LongConfirmationThreshold` or raising `ShortConfirmationThreshold` will reduce the number of trades but improve
  selectivity.
