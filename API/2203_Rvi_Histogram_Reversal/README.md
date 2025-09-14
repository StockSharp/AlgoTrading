# RVI Histogram Reversal Strategy

## Overview

This strategy trades against extreme RVI values. It operates on the Relative Vigor Index (RVI) and opens positions when the indicator leaves overbought or oversold zones or when the RVI crosses its signal line. Two signal modes are supported:

- **Levels** – reacts to RVI crossing predefined upper or lower thresholds.
- **Cross** – reacts to RVI crossing its signal line.

The logic is contrarian: if RVI was above the high level and then falls back, a long position is opened. If RVI was below the low level and then rises back, a short position is opened.

## Parameters

| Name | Description |
| --- | --- |
| `RviPeriod` | RVI calculation period. |
| `HighLevel` | Upper threshold for the RVI. |
| `LowLevel` | Lower threshold for the RVI. |
| `Mode` | Signal generation mode (`Levels` or `Cross`). |
| `EnableBuyOpen` | Allow opening long positions. |
| `EnableSellOpen` | Allow opening short positions. |
| `EnableBuyClose` | Allow closing long positions. |
| `EnableSellClose` | Allow closing short positions. |
| `CandleType` | Candle time frame. |

## How It Works

1. RVI and its simple moving average are calculated on every finished candle.
2. Depending on the selected mode, the strategy checks for:
   - the RVI leaving an extreme level, or
   - the RVI crossing its signal line.
3. On a long signal the strategy closes short positions and opens a long one. On a short signal it closes long positions and opens a short one.

The default time frame is four hours.

## Notes

- Orders are executed with market orders.
- Stop-loss and take-profit management can be added separately if required.
