# Three EMA Strategy

## Overview
The strategy reproduces the MetaTrader "ThreeEMA" expert advisor by stacking three exponential moving averages (EMAs). It looks for directional alignment between a fast, medium, and slow EMA on the same timeframe. When the averages are strictly ordered in ascending fashion (fast above medium above slow) the strategy opens or maintains a long position. When the order flips (fast below medium below slow) it opens or maintains a short position. Protective stop-loss and take-profit offsets mirror the original MQL parameters and are expressed in price points relative to the instrument's tick size.

## Original MQL behaviour
The MQL version instantiated three EMA indicators (`FastPeriod`, `MediumPeriod`, `SlowPeriod`) and generated trading signals based on their relative ordering on the most recently closed bar:

- **Open long / close short** when `FastEMA > MediumEMA > SlowEMA`.
- **Open short / close long** when `FastEMA < MediumEMA < SlowEMA`.
- Stop-loss and take-profit were applied as fixed distances in points from the entry price.

Orders were submitted with market execution and the money management block used a fixed lot size. The trailing module was disabled.

## StockSharp implementation details
- Uses the high-level candle subscription API. Three `ExponentialMovingAverage` indicators are bound to the main timeframe subscription so every finished candle delivers all EMA values simultaneously.
- Trading decisions are evaluated only on fully formed candles to avoid intrabar noise.
- Whenever a directional stack appears, the strategy cancels any working orders, closes the opposite exposure if necessary, and opens a new market position in the required direction.
- `StartProtection` converts the configured point-based stop-loss and take-profit distances into actual price offsets using the instrument's `PriceStep`. This mirrors the protective behaviour from the original EA.
- Chart integration draws candles and all three EMAs when a chart area is available, making it easy to validate signals visually.

## Parameters
| Name | Default | Description |
|------|---------|-------------|
| `CandleType` | 1-minute time frame | Time frame of the candle subscription used for EMAs. |
| `FastPeriod` | 5 | Length of the fast EMA. Must be lower than `MediumPeriod`. |
| `MediumPeriod` | 12 | Length of the medium EMA. Must be between the fast and slow periods. |
| `SlowPeriod` | 24 | Length of the slow EMA. Must be the highest period value. |
| `StopLossPoints` | 400 | Protective stop-loss distance expressed in instrument points (converted to price using `PriceStep`). Set to zero to disable. |
| `TakeProfitPoints` | 900 | Take-profit distance in instrument points (converted to price using `PriceStep`). Set to zero to disable. |

## Usage notes
1. Configure `Volume` before starting the strategy to reflect the desired order size (the original EA used fixed lots).
2. Ensure the EMA periods remain strictly increasing; otherwise an exception is thrown during `OnStarted` to match the validation found in the MQL source.
3. Because the logic always flips positions when the EMA stack reverses, the strategy is continuously market-exposed whenever conditions alternate between bullish and bearish alignments.
