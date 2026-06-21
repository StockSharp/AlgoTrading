# Langfristig profitabler Swing-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie steigt long ein, wenn eine schnelle EMA eine langsame EMA nach oben kreuzt und der RSI über einem bestimmten Schwellenwert liegt. Ausstiege erfolgen, wenn der Kurs ATR-basierte Stop-Loss- oder Take-Profit-Niveaus erreicht.

## Details

- **Einstiegskriterien**:
  - Long: schnelle EMA kreuzt langsame EMA nach oben und RSI > Schwellenwert.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - Kurs erreicht ATR-basierten Stop-Loss oder Take-Profit.
- **Stops**: ATR-Vielfache für Stop-Loss und Take-Profit.
- **Standardwerte**:
  - `FastEmaLength` = 16
  - `SlowEmaLength` = 30
  - `RsiLength` = 9
  - `AtrLength` = 21
  - `RsiThreshold` = 50
  - `AtrStopMult` = 8
  - `AtrTpMult` = 11
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long
  - Indikatoren: EMA, RSI, ATR
  - Stops: Ja
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
