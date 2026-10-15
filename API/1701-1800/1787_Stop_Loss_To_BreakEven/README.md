# Stop Loss to Break-Even Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy moves the protective stop loss to the entry price once the position reaches a specified profit measured in pips. It is useful for locking in gains without manually adjusting orders.

## How It Works

- Monitors price using the selected candle type.
- When the current position profit exceeds the configured number of pips, a stop order is placed at the entry price.
- Works for both long and short positions and automatically uses the instrument's price step to calculate pip size.

## Parameters

| Name | Description |
| ---- | ----------- |
| **BreakEvenPips** | Profit in pips required before moving the stop loss to the entry price. |
| **CandleType** | Type of candles used for monitoring price movements. |

## Notes

The strategy does not generate entry signals. Positions should be opened by other strategies or manually. Once the position is closed, internal state is reset to await the next trade.
