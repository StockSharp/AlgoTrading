# PChannel System Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The **PChannel System** uses a price channel breakout with delayed confirmation. It tracks the highest high and lowest low over a configurable period. When price breaks through the channel and then closes back inside, the strategy enters in the direction of the breakout while closing any opposite positions. Optional stop-loss and take-profit levels manage risk.

## Parameters
- `Period` – lookback length for the channel.
- `Shift` – number of bars to delay channel values.
- `StopLoss` – absolute price distance for the protective stop.
- `TakeProfit` – absolute price distance for the profit target.
- `CandleType` – candle series used for calculations.

## Trading logic
1. Compute channel bounds from the last `Period` candles with an optional `Shift`.
2. If the previous candle closed outside the channel and the current candle returns inside, open a position in the breakout direction.
3. Close the opposite position, if any, before opening a new one.
4. Monitor active trades and exit when `StopLoss` or `TakeProfit` is reached.

This strategy has no Python implementation yet.
