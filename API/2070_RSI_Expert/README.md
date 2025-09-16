# RSI Expert

## Overview

The **RSI Expert** strategy trades using the Relative Strength Index (RSI). It waits for the RSI value to cross predefined overbought or oversold levels and enters positions in the direction of the crossing.

## Logic

- Calculate RSI for each candle.
- When RSI crosses **above** the oversold level, a long position is opened.
- When RSI crosses **below** the overbought level, a short position is opened.
- Before entering a new position the opposite one is closed.
- Optional protections for take‑profit, stop‑loss and trailing stop can be enabled.

The strategy processes only **finished candles** and uses StockSharp's high‑level API with indicator binding.

## Parameters

| Name | Description | Default |
|------|-------------|---------|
| `RsiPeriod` | RSI calculation period. | `14` |
| `LevelUp` | Overbought level to trigger shorts. | `70` |
| `LevelDown` | Oversold level to trigger longs. | `30` |
| `TakeProfitPercent` | Take profit percentage. `0` disables. | `0` |
| `StopLossPercent` | Stop loss percentage. `0` disables. | `0` |
| `TrailingStopPercent` | Trailing stop percentage. `0` disables. | `0` |
| `CandleType` | Candle timeframe used for calculations. | `1 minute` |

## Notes

The trailing stop uses the built‑in `StartProtection` mechanism. When `TrailingStopPercent` is greater than zero it replaces the regular stop loss and automatically follows the price.
