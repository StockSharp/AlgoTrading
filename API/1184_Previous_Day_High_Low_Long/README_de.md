# Strategie für Long bei Vortages-Hoch/Tief
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie geht Long, wenn der Preis während einer bestimmten Sitzung über das Hoch oder Tief des Vortages bricht und der ADX einen sich stärkenden Aufwärtsmomentum anzeigt.

## Details

- **Einstiegskriterien**:
  - **Long**: Schluss kreuzt über das Hoch oder Tief des Vortages mit steigendem ADX während der Sitzung.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - Dynamischer Stop und Gewinnziele oder am Sitzungsende.
- **Stops**: Trailing-Stop.
- **Standardwerte**:
  - `MaxProfit` = 150.
  - `MaxStopLoss` = 15.
  - `AdxLength` = 11.
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame().
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Nur Long
  - Indikatoren: ADX
  - Stops: Ja
  - Komplexität: Moderat
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
