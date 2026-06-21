# Turtle Trading System
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Klassisches Turtle Trading System mit Donchian-Kanal-Ausbrüchen und ATR-basiertem Risikomanagement.

## Details

- **Einstiegskriterien**: Ausbruch über das obere/untere Band des Donchian-Kanals
- **Long/Short**: beide
- **Ausstiegskriterien**: Kreuzung des kürzeren Donchian-Kanals oder Trailing-Stop
- **Stops**: ATR-basierter Initial- und Trailing-Stop
- **Standardwerte**:
  - `EntryLength` = 20
  - `ExitLength` = 10
  - `EntryLengthMode2` = 55
  - `ExitLengthMode2` = 20
  - `AtrPeriod` = 14
  - `RiskPerTrade` = 0.02
  - `InitialStopAtrMultiple` = 2
  - `PyramidAtrMultiple` = 0.5
  - `MaxUnits` = 4
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: DonchianChannels, ATR
  - Stops: ATR
  - Komplexität: Fortgeschritten
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
