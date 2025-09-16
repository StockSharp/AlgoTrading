# Auto Trailing Stop
[Русский](README_ru.md) | [中文](README_cn.md)

Automatically attaches stop-loss and take-profit orders to existing positions and trails the stop as price moves in favor.

## Details
- **Entry Criteria**: None, the strategy does not open trades.
- **Long/Short**: Works with both long and short positions already open.
- **Exit Criteria**: Stop-loss and take-profit orders. Trailing stop updates after price moves by half of trailing distance.
- **Stops**: Initial stop-loss and take-profit placed when position appears; stop loss trails by `TrailingStopStep`.
- **Default Values**: TrailingStop 6, TrailingStopStep 1, TakeProfit 35, StopLoss 114.
- **Filters**: Optional disabling of trailing stop, automatic take profit, or automatic stop loss via parameters.

## Parameters
- `FridayTrade` - allow trailing on Fridays.
- `UseTrailingStop` - enable trailing stop logic.
- `AutoTrailingStop` - use default trailing distance of 6 when true.
- `TrailingStop` - trailing distance in price units when AutoTrailingStop is false.
- `TrailingStopStep` - minimum price movement before trailing stop is moved.
- `AutomaticTakeProfit` - automatically place take profit order.
- `TakeProfit` - take profit distance.
- `AutomaticStopLoss` - automatically place stop loss order.
- `StopLoss` - stop loss distance.
- `CandleType` - candle type for price updates (default 1-minute).
