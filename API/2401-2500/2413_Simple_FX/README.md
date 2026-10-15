# Simple FX Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Overview
This strategy uses two exponential moving averages to detect trend changes. A long position is opened when the short EMA crosses above the long EMA, while a short position is opened when the short EMA crosses below the long EMA.

## Parameters
- **Long MA Period** – period of the long EMA.
- **Short MA Period** – period of the short EMA.
- **Stop Loss (points)** – protective stop in price steps.
- **Take Profit (points)** – profit target in price steps.
- **Candle Type** – timeframe for candles.
