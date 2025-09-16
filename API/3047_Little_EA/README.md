# Little EA Strategy

## Overview
Little EA is a moving-average crossover expert originally written for MetaTrader. The strategy observes the candle selected by the **OHLC bar index** parameter and reacts when that candle crosses a shifted moving average from below or above. The StockSharp port keeps the original multi-entry idea by allowing several tranches per direction while respecting a configurable maximum exposure.

## Trading logic
1. Subscribe to the configured candle series and feed the selected moving average type with the chosen price source (close, open, high, low, median, typical, or weighted).
2. Store completed candles so the strategy can reference the candle at the `OhlcBarIndex` (the default value `1` means the last fully closed candle).
3. Apply the optional `MaShift` by reading the moving-average value from several bars back, replicating the MetaTrader visual shift.
4. When the reference candle closes above the shifted MA, treat it as a bullish cross. When it closes below the shifted MA, treat it as a bearish cross.
5. For a bullish cross:
   - If the net short exposure already equals the configured maximum, close the entire short position.
   - Otherwise, if the long exposure is still below the maximum, add one `TradeVolume` tranche to the long side.
6. For a bearish cross:
   - If the long exposure already equals the maximum, close the entire long position.
   - Otherwise, if the short exposure is below the limit, add one `TradeVolume` tranche to the short side.

The volume cap emulates the original expert’s `Int_Max_Pos` limit while working with StockSharp’s net positions.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 1-minute time frame | Primary timeframe used for signals and indicator calculations. |
| `OhlcBarIndex` | `int` | `1` | Index of the historical candle used for crossover detection (0 = current forming candle, 1 = last finished candle). |
| `MaxPositionsPerSide` | `int` | `15` | Maximum number of `TradeVolume` tranches that can be accumulated per direction. |
| `MaPeriod` | `int` | `64` | Length of the moving average. |
| `MaShift` | `int` | `0` | Number of bars to shift the MA backwards when checking crossovers. |
| `MaType` | `MovingAverageType` | `Smoothed` | Moving-average calculation mode (Simple, Exponential, Smoothed, Weighted). |
| `AppliedPrice` | `AppliedPriceType` | `Close` | Price source used as indicator input. |
| `TradeVolume` | `decimal` | `1` | Order volume sent with every new tranche. |

## Differences from the original MetaTrader expert
- Money management is simplified: only fixed volume entries are supported. Percent-risk sizing from the original EA is not implemented.
- StockSharp works with net positions, so opposite-direction positions are flattened before new exposure is accumulated. The `MaxPositionsPerSide` limit is enforced on the net exposure in lots.
- Indicator values and candle history are processed through the high-level candle subscription API rather than manual buffer copies.

## Usage tips
- Adjust `TradeVolume` to match the instrument’s lot step before launching the strategy; the constructor also assigns the same value to `Strategy.Volume` so helper methods use the desired size by default.
- Use `MaShift` in combination with `OhlcBarIndex` to recreate the visual alignment from the MetaTrader chart if needed.
- Add the strategy to a chart to view candles, the moving-average overlay, and executed trades, which helps with verifying crossover behavior.

## Indicators
- One configurable moving average (`Simple`, `Exponential`, `Smoothed`, or `Weighted`).
