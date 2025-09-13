# Breakdown Level Day Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy places pending stop orders above and below the intraday range at a specified time of day. It aims to capture breakouts when price moves beyond the early session high or low. Optional stop loss, take profit, break-even and trailing stop rules manage the open position.

## Details

- **Entry**: At `OrderTime` a buy stop is placed above the day's high plus `Delta` ticks and a sell stop below the day's low minus `Delta` ticks.
- **Exit**: Stop-loss and take-profit orders are placed together with the breakout order. Break-even and trailing stop can update the protective stop.
- **Indicators**: None.
- **Timeframe**: Default 1-minute candles.
- **Risk**: Position size is taken from the strategy `Volume` property.

## Parameters

- `OrderTime` — time of day when pending orders are submitted.
- `Delta` — distance from range boundaries in ticks.
- `StopLoss` — protective stop distance in ticks.
- `TakeProfit` — profit target distance in ticks.
- `BreakEven` — move stop to entry after this profit in ticks.
- `Trailing` — trailing stop distance in ticks.
- `CandleType` — candle type used for calculations.
