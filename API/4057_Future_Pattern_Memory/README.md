# FuturePatternMemoryStrategy

## Overview
`FuturePatternMemoryStrategy` is a StockSharp port of the classic MetaTrader experts **FutureMA** and **FutureMACD**. The original robots recorded sequences of indicator differences into CSV files, reused the stored statistics, and decided whether current conditions favored bullish or bearish breakouts. This C# version keeps the same idea, but replaces the file system with an in-memory pattern warehouse and exposes every knob as a strategy parameter. The strategy can operate either on the smoothed moving average spread (the FutureMA logic) or on the MACD histogram (the FutureMACD logic).

The strategy evaluates each finished candle in five stages:

1. **Indicator projection** – compute the selected oscillator (MA spread or MACD histogram) and normalise it with a configurable scaling factor. The values are discretised to integers to create compact pattern signatures.
2. **Pattern hashing** – maintain a sliding window of the latest `AnalysisBars` normalised values. Every time a new bar closes, the window is converted into a unique hash string that identifies the current market context.
3. **Historical swing analysis** – inspect the previous `FractalDepth` candles, measure the distance between the oldest open and the highest/lowest extremes, and convert those ranges to points. These distances are the reward expectations that the original robots accumulated in their CSV files.
4. **Weighted memory update** – the hash key is used to retrieve or create an entry in the pattern dictionary. The bullish and bearish take-profit expectations are updated with a weighted moving average controlled by `ForgettingFactor`, which reproduces the “forgetfulness” coefficient (`zabyvaemost`) from the MQL code.
5. **Signal evaluation and execution** – if the bullish expectation dominates, the pattern was seen more than `MinimumMatches` times, and the projected gain is above `MinimumTakeProfit`, the strategy enters or adds to a long position. The bearish branch works symmetrically. Protective levels are derived from the stored statistics and optionally trailed as the trade moves in favour.

## Conversion notes
- Both MetaTrader experts are merged into one configurable strategy via the `Source` parameter, allowing you to switch between the MA-based engine and the MACD-based engine without recompilation.
- File-based persistence was replaced with a `Dictionary<string, PatternStats>` that keeps all statistics in memory during the run. This avoids file I/O and stays within the StockSharp sandbox model.
- Position management replicates the original stop/target placement: the stop uses the full averaged swing, while the take-profit uses `StatisticalTakeRatio` (the original `Stat_Take_Profit`). When `EnableTrailingStop` is true the stop is moved in quarter steps of the profit distance, exactly as the MQL expert modified its orders.
- Manual mode (`ManualMode`) disables automated order placement but continues to collect statistics, matching the original `Ruchnik` flag behaviour.
- Scaling in (`AllowAddOn`) mimics the `dokupka` flag and allows the strategy to add volume whenever the pattern repeats on a new bar.

## Trading logic in detail
- **Indicator source**
  - *MA spread*: calculates two smoothed moving averages (SMMA 6 and SMMA 24) on median prices and uses their difference.
  - *MACD histogram*: calculates the difference between the MACD main line and the signal line (12/26/9 configuration by default).
- **Normalisation**: `NormalizationFactor` reproduces `tocnost`; it scales the raw difference before converting it to an integer signature. The conversion divides by `100 * MinPriceStep` to maintain pip-based units.
- **Pattern memory**: the dictionary stores, for each signature, the number of bullish matches, the average bullish distance, the number of bearish matches, and the average bearish distance. Values are updated with the weighted formula `(current + input * ForgettingFactor) / (1 + ForgettingFactor)`.
- **Entry rules**:
  - Long: bullish expectation ≥ bearish expectation, bullish matches > `MinimumMatches`, expected bullish distance > `MinimumTakeProfit`.
  - Short: bearish expectation ≥ bullish expectation, bearish matches > `MinimumMatches`, expected bearish distance > `MinimumTakeProfit`.
- **Risk management**: stop-loss is set to one full averaged swing against the position; take-profit uses `StatisticalTakeRatio` of that swing. Trailing stops move after price travels one quarter of the distance, just like the original trailing routine.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `CandleType` | Main timeframe used for calculations. | 30-minute candles |
| `Source` | Choose between MA spread (`FutureMA`) and MACD histogram (`FutureMACD`). | `MaSpread` |
| `FastMaLength` / `SlowMaLength` | SMMA lengths when `Source = MaSpread`. | 6 / 24 |
| `MacdFastLength` / `MacdSlowLength` / `MacdSignalLength` | MACD periods when `Source = MacdHistogram`. | 12 / 26 / 9 |
| `AnalysisBars` | Number of bars forming the pattern signature. | 8 |
| `FractalDepth` | Number of past candles used to measure breakout distances. | 4 |
| `MinimumMatches` | Required number of stored occurrences before taking a trade. | 5 |
| `MinimumTakeProfit` | Minimum expected distance (in points) to accept the signal. | 30 |
| `NormalizationFactor` | Scaling factor applied to the indicator difference. | 10 |
| `ForgettingFactor` | Weight applied to new measurements in the pattern memory. | 1.5 |
| `StatisticalTakeRatio` | Take-profit ratio relative to the measured swing. | 0.5 |
| `EnableTrailingStop` | Enables quarter-step trailing stop logic. | `false` |
| `ManualMode` | Collect statistics but skip order placement. | `false` |
| `AllowAddOn` | Permit scaling in when a pattern repeats. | `true` |
| `Volume` | Order size used for entries. | 0.1 |

## Practical advice
- The strategy relies on discretised hashes, so choose `NormalizationFactor` and `AnalysisBars` carefully: too large values produce sparse signatures, while too small values blend distinct states together.
- When running in live trading, consider exporting the pattern dictionary after the session if you need persistence between runs.
- Because the MQL version stored data per symbol/period, it is recommended to keep a dedicated strategy instance per instrument/timeframe to avoid cross-contamination of statistics.
