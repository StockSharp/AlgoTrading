# Bronze Pan Strategy

This strategy is a StockSharp port of the MetaTrader 4 expert advisor "Bronzew_pan". It trades a single instrument on finished candles and combines the proprietary DayImpuls oscillator with Williams %R and the Commodity Channel Index (CCI) to detect momentum reversals.

## How it works

1. Subscribes to the configured candle type and runs DayImpuls, Williams %R and CCI with the same period.
2. Keeps independent accounting of long and short exposures to emulate the original hedging behaviour.
3. Closes all positions once the floating profit reaches `ProfitTarget` or drops below `LossTarget`.
4. Opens a short when DayImpuls stays above `DayImpulsShortLevel` and declines, while Williams %R is above `WilliamsLevelUp` and CCI exceeds `CciLevel`.
5. Opens a long when DayImpuls stays below `DayImpulsLongLevel` and rises, while Williams %R is below `WilliamsLevelDown` and CCI is less than `-CciLevel`.
6. If the floating PnL moves beyond the `PredBand` bounds, the strategy sends a large averaging order multiplied by `LotMultiplier` to flip direction, mirroring the emergency recovery logic from MetaTrader.
7. Individual stop-loss and take-profit values are monitored for long and short baskets using pip distances converted to prices.
8. No new trades are opened when the account balance falls below `MinimumBalance` or when both long and short baskets are active.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `TradeVolume` | Base volume for entries. | `0.1` |
| `LongStopLossPips` | Stop-loss distance for long baskets in pips. | `0` |
| `ShortStopLossPips` | Stop-loss distance for short baskets in pips. | `0` |
| `LongTakeProfitPips` | Take-profit distance for long baskets in pips. | `0` |
| `ShortTakeProfitPips` | Take-profit distance for short baskets in pips. | `0` |
| `IndicatorPeriod` | Length used by DayImpuls, Williams %R and CCI. | `14` |
| `CciLevel` | Absolute CCI threshold confirming overbought/oversold. | `150` |
| `WilliamsLevelUp` | Williams %R level required for shorts. | `-15` |
| `WilliamsLevelDown` | Williams %R level required for longs. | `-85` |
| `DayImpulsShortLevel` | DayImpuls level that enables short entries. | `50` |
| `DayImpulsLongLevel` | DayImpuls level that enables long entries. | `-50` |
| `ProfitTarget` | Floating profit that closes every position. | `500` |
| `LossTarget` | Floating loss that closes every position. | `-2000` |
| `PredBand` | Profit band used to trigger averaging reversals. | `100` |
| `LotMultiplier` | Multiplier applied to the base volume during reversals. | `30` |
| `MinimumBalance` | Minimal account balance required to keep trading. | `3000` |
| `CandleType` | Time-frame used for candle subscriptions. | `15m` |

## Notes

- The DayImpuls oscillator replicates the original double EMA smoothing over candle bodies expressed in points.
- Stop-loss and take-profit values are optional; setting `0` disables the respective protection side.
- The strategy relies on finished candles and ignores incomplete bars.
