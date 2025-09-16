# Trailing Stop EA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy manages an existing position by applying a trailing stop. It listens to tick trades and shifts the stop level as price moves in a favorable direction. When the market reverses and hits the trailing level, the strategy exits the position.

## Details

- **Entry**: The strategy does not open positions; it assumes a position is already open.
- **Long logic**: For long positions, once price rises by the trailing distance, the stop follows price upward.
- **Short logic**: For short positions, the stop moves downward as price falls.
- **Exit**: Position is closed when price reaches the trailing stop.
- **Indicators**: None.
- **Timeframe**: Tick-based, reacts to each trade.
- **Stops**: Trailing stop only.

## Parameters

- `TrailingPoints` — trailing distance in points (price steps). Default: 200.
