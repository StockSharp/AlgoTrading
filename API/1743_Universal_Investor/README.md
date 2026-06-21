# Universal Investor Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Overview

The **Universal Investor Strategy** uses the crossover between the Exponential Moving Average (EMA) and the Linear Weighted Moving Average (LWMA) to determine market direction. It confirms trend strength by checking that both averages move in the same direction.

## Logic

- **Buy Entry**: LWMA is above EMA and both averages are rising.
- **Sell Entry**: LWMA is below EMA and both averages are falling.
- **Buy Exit**: LWMA crosses below EMA.
- **Sell Exit**: LWMA crosses above EMA.

The strategy reduces position size after consecutive losing trades when the decrease factor is enabled.

## Parameters

| Name | Description |
| ---- | ----------- |
| `MovingPeriod` | Length for EMA and LWMA calculations. |
| `DecreaseFactor` | Lot reduction factor after losses (0 disables reduction). |
| `CandleType` | Candle data type for calculations. |
| `Volume` | Base trade volume from the strategy settings. |

## Notes

- Works on finished candles only.
- Uses high-level StockSharp API with indicator binding.
- No Python version is provided.

