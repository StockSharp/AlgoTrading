# WPR Histogram Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy trades based on the behavior of the Williams %R indicator. It monitors when the indicator leaves overbought or oversold zones and enters positions in the opposite direction.

## Logic

- When Williams %R rises above the high level and then drops back, it is considered a signal that the market leaves the overbought zone. The strategy opens a long position.
- When Williams %R falls below the low level and then rises back, the market leaves the oversold zone. The strategy opens a short position.
- Existing opposite positions are closed before opening a new one.

## Parameters

- **WPR Period** – period of Williams %R calculation.
- **High Level** – threshold for the overbought zone.
- **Low Level** – threshold for the oversold zone.
- **Candle Type** – type and timeframe of candles used for calculations.

## Notes

The strategy uses only market orders and does not set stop-loss or take-profit levels. It relies on the user-defined `Volume` property for position sizing.
