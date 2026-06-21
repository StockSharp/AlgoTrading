# Sigma-Spike-Strategie nach Tageszeit / Wochentag
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Verwendet den Rendite-Z-Score, um große Bewegungen nach Stunden mit optionalen Tagesfiltern hervorzuheben.
Kauft bei Spitzen und steigt aus, wenn die Volatilität sich normalisiert.

## Details

- **Einstiegskriterien**: absoluter Z-Score >= `Threshold`
- **Long/Short**: Nur Long
- **Ausstiegskriterien**: Z-Score fällt unter `Threshold`
- **Stops**: Nein
- **Standardwerte**:
  - `Threshold` = 2.5
  - `AllDays` = false
  - `DayOfWeekFilter` = Monday
  - `StdevLength` = 20
- **Filter**:
  - Kategorie: Volatilität
  - Richtung: Long
  - Indikatoren: StandardDeviation
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Ja
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
