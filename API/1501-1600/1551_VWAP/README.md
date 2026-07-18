# VWAP Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Uses VWAP with entry bands and multiple exit modes. Buys when price closes above the lower band and sells when price closes below the upper band. Supports VWAP or deviation band exits and optional safety exit after consecutive opposing bars.

## Parameters

- **StopPoints**: Stop buffer from signal bar.
- **ExitModeLong**: Exit mode for long positions.
- **ExitModeShort**: Exit mode for short positions.
- **TargetLongDeviation**: Deviation multiplier for long target.
- **TargetShortDeviation**: Deviation multiplier for short target.
- **EnableSafetyExit**: Enable safety exit after opposing bars.
- **NumOpposingBars**: Number of opposing bars for safety exit.
- **AllowLongs**: Allow long trades.
- **AllowShorts**: Allow short trades.
- **MinStrength**: Minimum signal strength.
- **CandleType**: Type of candles.

