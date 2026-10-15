# ThinkTech AI Signals-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Ausbrüche der ersten 15-Minuten-Kerze der Sitzung. Sie verwendet ATR-basierte Stop-Loss- und Take-Profit-Niveaus und kann optionale Trend- und RSI-Filter anwenden.

## Details

- **Einstiegskriterien**:
  - **Long**: Preis bricht über das Hoch der ersten Kerze, wenn Trend- und RSI-Filter erfüllt sind.
  - **Short**: Preis bricht unter das Tief der ersten Kerze, wenn Trend- und RSI-Filter erfüllt sind.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Take-Profit- oder Stop-Loss-Niveau erreichen.
- **Stops**: Ja, ATR-basiert.
- **Standardwerte**:
  - `RiskRewardRatio` = 2
  - `AtrLength` = 14
  - `AtrMultiplier` = 1.5
  - `RsiPeriod` = 14
  - `RsiOversold` = 30
  - `RsiOverbought` = 70
