# Volume per Point Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy calculates the volume per price point for each candle. A long trade is opened when the candle range decreases but volume increases and the RSI filter (if enabled) confirms the signal. A short trade is opened when the range expands while volume contracts.

## Parameters
- **RSI Length** – period for RSI calculation.
- **RSI Above/Below** – thresholds for the optional RSI filter.
- **Use RSI Filter** – enable or disable RSI filtering.
- **Candle Type** – timeframe of input candles.
