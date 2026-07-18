# Dskyz (DAFE) GENESIS-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Vereinfachte Version der Dskyz (DAFE) GENESIS-Strategie. Das System handelt, wenn kurzfristiger Momentum mit einem Trendfilter und RSI übereinstimmt.

## Details

- **Einstiegskriterien**:
  - **Long**: `SMA(9) > SMA(30)` und `RSI > 55` und `EMA(8) > EMA(21)`.
  - **Short**: `SMA(9) < SMA(30)` und `RSI < 45` und `EMA(8) < EMA(21)`.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**:
  - **Long**: `EMA(8) < EMA(21)`.
  - **Short**: `EMA(8) > EMA(21)`.
- **Stops**: Keine.
- **Standardwerte**:
  - `RSI Length` = 9.
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: RSI, EMA, SMA
  - Stops: Nein
  - Komplexität: Einfach
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
