# VWAP-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Verwendet VWAP mit Einstiegsbändern und mehreren Ausstiegsmodi. Kauft, wenn der Preis über dem unteren Band schließt, und verkauft, wenn er unter dem oberen Band schließt. Unterstützt Ausstiege per VWAP oder Abweichungsband sowie einen optionalen Sicherheitsausstieg nach aufeinanderfolgenden Gegenbewegungskerzen.

## Parameter

- **StopPoints**: Stop-Puffer von der Signalkerze.
- **ExitModeLong**: Ausstiegsmodus für Long-Positionen.
- **ExitModeShort**: Ausstiegsmodus für Short-Positionen.
- **TargetLongDeviation**: Abweichungsmultiplikator für Long-Ziel.
- **TargetShortDeviation**: Abweichungsmultiplikator für Short-Ziel.
- **EnableSafetyExit**: Sicherheitsausstieg nach Gegenbewegungskerzen aktivieren.
- **NumOpposingBars**: Anzahl der Gegenbewegungskerzen für den Sicherheitsausstieg.
- **AllowLongs**: Long-Trades erlauben.
- **AllowShorts**: Short-Trades erlauben.
- **MinStrength**: Minimale Signalstärke.
- **CandleType**: Kerzentyp.
