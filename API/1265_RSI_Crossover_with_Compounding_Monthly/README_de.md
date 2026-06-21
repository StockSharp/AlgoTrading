# RSI-Kreuzungs-Strategie mit Zinseszins (Monatlich)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie investiert das gesamte Kapital, wenn der monatliche RSI über seinem SMA schließt, und steigt aus, wenn RSI unter den SMA fällt. Gewinne werden dem Kapital für den Zinseszinseffekt hinzugefügt.

Backtests deuten auf eine durchschnittliche Jahresrendite von etwa 20% hin. Funktioniert am besten bei Aktien.

## Details

- **Einstiegskriterien**: RSI über seinem SMA
- **Long/Short**: Long
- **Ausstiegskriterien**: RSI unter seinem SMA
- **Stops**: Nein
- **Standardwerte**:
  - `CandleType` = 1 Monat
  - `RsiPeriod` = 14
  - `InitialCapital` = 100000
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Nur Long
  - Indikatoren: RSI, SMA
  - Stops: Nein
  - Komplexität: Anfänger
  - Zeitrahmen: Monatlich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
