# MA Mirror Strategy

## Overview
The MA Mirror strategy is a conversion of the MetaTrader expert *MA_MirrorEA*. The system compares two simple moving averages
calculated on the same period but using different price sources: candle closes versus candle opens. When the moving average of
closing prices stays above the moving average of opening prices the strategy wants to be long; when it drops below the opening
average the strategy wants to be short. A configurable shift parameter allows the moving averages to be read from older candles
so the StockSharp port can reproduce the visual displacement applied in the original MetaTrader indicator.

The StockSharp implementation keeps the original “mirror” behaviour: only one market position can exist at any time, and a
signal change first closes the previous position and then opens a new one in the opposite direction. Just like the MetaTrader
code, the strategy begins with a virtual short signal, meaning the very first real trade happens only after the close average
moves above the open average.

## Trading logic
1. Subscribe to the candle series defined by `CandleType` and process only finished candles to avoid premature decisions.
2. Feed two simple moving averages with the candle close and open prices. Both indicators share the same `MovingPeriod` so their
   values can be compared directly.
3. Store the recent moving-average values in ring buffers. The buffers make it possible to retrieve the value from `MovingShift`
   candles ago, emulating the MetaTrader shift parameter without calling forbidden indicator methods.
4. When the shifted close average is above the shifted open average, set the desired signal to **buy**. When it is below, set the
   desired signal to **sell**. If both averages are equal the previous signal is preserved.
5. If this is the first signal and it is not bullish, remain flat. Otherwise, if the desired signal differs from the last executed
   signal, close any existing exposure and open a new market position with `TradeVolume` lots in the new direction.
6. Update the stored signal so later candles ignore duplicate instructions while the position direction remains unchanged.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 1-minute time frame | Primary timeframe processed by the strategy. |
| `MovingPeriod` | `int` | `20` | Length of the simple moving averages used on close and open prices. |
| `MovingShift` | `int` | `0` | Number of completed candles that the moving-average values are shifted backwards. |
| `TradeVolume` | `decimal` | `1` | Quantity used for every market order. |

## Differences from the original MetaTrader expert
- The money-management helpers (stop loss, take profit, trailing stop) contained in the MQL include file are not ported. The
  StockSharp version always trades a fixed `TradeVolume` and relies on external risk controls if needed.
- MetaTrader stores individual orders, while StockSharp works with net positions. The conversion closes the existing net position
  before opening a new one so the resulting exposure matches the EA’s single-ticket behaviour.
- Indicator processing is handled through StockSharp’s candle subscription API together with `SimpleMovingAverage` indicators and
  internal buffers instead of calling `iMA` directly.

## Usage tips
- Adjust `TradeVolume` to the instrument’s lot step before starting the strategy. The constructor also assigns the same value to
  `Strategy.Volume`, so helper methods issue orders with the expected size.
- Increase `MovingShift` if you want to read the moving averages from older candles, for example to align with how the MetaTrader
  platform plots shifted indicators.
- Add the strategy to a chart to visualise candles together with both moving averages and executed trades, which makes it easier
  to confirm that reversals occur exactly when the close average crosses the open average.

## Indicators
- Two simple moving averages (close price and open price) with identical lengths and optional backward shift.
