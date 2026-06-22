# Fisher Org v1 Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy uses the Fisher Transform indicator to capture trend reversals. A long position is opened when the indicator forms a local minimum, while a short position is opened when a local maximum appears. Opposite signals close any existing position.

## Rules
- **Long**: `Fisher[t-2] > Fisher[t-1]` and `Fisher[t-1] <= Fisher[t]`
- **Short**: `Fisher[t-2] < Fisher[t-1]` and `Fisher[t-1] >= Fisher[t]`

## Parameters
- `Fisher Length` – period of the Fisher Transform (default 7)
- `Candle Type` – timeframe of candles used for calculations

## Indicators
- Fisher Transform
