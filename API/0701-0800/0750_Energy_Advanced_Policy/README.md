# Energy Advanced Policy Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

The **Energy Advanced Policy** strategy combines policy sentiment with basic technical filters.

- **Long**: EMA(21) above EMA(55), RSI below overbought, Bollinger Bands not in squeeze.
- **Exit**: RSI crosses above overbought or EMA trend reverses.

## Parameters
- `NewsSentiment` – manual sentiment.
- `EnableNewsFilter` – enable policy sentiment override.
- `EnablePolicyDetection` – allow policy event detection.
- `PolicyVolumeThreshold` – volume spike multiple.
- `PolicyPriceThreshold` – price change threshold (%).
- `RsiLength` – RSI period.
- `RsiOverbought` – RSI overbought level.
- `FastLength` – fast EMA period.
- `SlowLength` – slow EMA period.
- `BbLength` / `BbMult` – Bollinger Bands settings.

Indicators: RSI, EMA, Bollinger Bands.
