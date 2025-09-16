# Line Order Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The **Line Order Strategy** triggers a market order when price crosses a user-defined horizontal line. It is intended as a simplified conversion of the original MQL script *LineOrder.mq4*, providing manual line trading functionality through the high-level StockSharp API.

The strategy exposes parameters to control direction, entry level and risk management. After entering a position, optional stop-loss, take-profit and trailing stop levels are monitored on every completed candle. The logic is fully event-driven and does not maintain custom collections.

## Parameters
- **LinePrice** – price level for placing the order.
- **IsBuy** – `true` for long entries, `false` for short entries.
- **StopLoss** – stop-loss distance in price units (0 disables).
- **TakeProfit** – take-profit distance in price units (0 disables).
- **TrailingStop** – trailing stop distance in price units (0 disables).
- **Volume** – order volume.
- **CandleType** – candle type used to monitor price.

## Trading Rules
- **Entry**: when the closing price crosses the `LinePrice` in the chosen direction.
- **Stop-loss**: closes position when loss exceeds `StopLoss` distance from entry.
- **Take-profit**: closes position when profit reaches `TakeProfit` distance.
- **Trailing stop**: after entry, adjusts to the most favorable price and exits when price moves against position by `TrailingStop`.

## Notes
- Works with any security supported by StockSharp.
- Designed for educational purposes to illustrate translation of manual line trading from MQL.
- Python version is intentionally omitted.
