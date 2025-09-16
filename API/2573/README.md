# e_RP_250 Reverse Point Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy is a StockSharp port of the MetaTrader 5 expert advisor `e_RP_250`. The original system trades reversals detected by
a custom *rPoint* indicator. Because that indicator is not available inside StockSharp, the conversion recreates the same behaviour
with rolling highest and lowest price trackers. Whenever a new swing high or swing low appears, the strategy reverses the position
and attaches the same stop-loss, take-profit and optional trailing logic as the MQL version.

The original source did not publish verified performance results, so you should perform your own evaluation before deploying the
strategy in production.

## Trading logic

- Subscribe to candles defined by the `CandleType` parameter (5-minute candles by default).
- Track the highest high and lowest low across the last `ReversePoint` bars (250 by default).
- When the current candle sets a new highest high, close any long position and open a short position.
- When the current candle sets a new lowest low, close any short position and open a long position.
- Protective stop-loss and take-profit levels are expressed in price points and are reproduced through `StartProtection`.
- Optional trailing stops lock in profits once price moves by the configured number of points.

Only one position is active at any time. The strategy also blocks duplicate orders during the same candle by remembering the last
execution time, replicating the `TimeN` safeguard from the MQL script.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `TakeProfitPoints` | Distance in price points for the take-profit order (default **15**). Set to zero to disable automatic profit taking. |
| `StopLossPoints` | Distance in price points for the stop-loss order (default **999**). Set to zero to trade without a fixed stop. |
| `TrailingStopPoints` | Optional trailing stop distance in price points (default **0** disables the trailing logic). |
| `ReversePoint` | Number of candles used to detect reversal points. Larger values react slower but filter out noise. |
| `CandleType` | Candle aggregation to analyse. Default is a 5-minute time frame but you can switch to any `DataType`. |

## Position management

- `StartProtection` applies the same stop-loss and take-profit distances as the MT5 expert.
- The trailing stop tracks the most favourable price after entry and exits when price reverts by the configured amount.
- Reversal signals from the opposite side immediately close the current position before opening a new one.

## Usage notes

- Make sure the data source supports the selected candle type, otherwise no signals will be generated.
- The strategy relies on decimal prices. Verify that the security's `PriceStep` property correctly reflects the point value.
- Test different `ReversePoint` values to adapt the breakout sensitivity to the volatility of the traded instrument.
