# Varanormal Mac N Cheez-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

SMA-Crossover-Strategie mit Trailing-Stop und täglichem Gewinnziel.

## Details

- **Einstiegskriterien**:
  - **Long**: Schneller SMA kreuzt über den langsamen SMA.
  - **Short**: Schneller SMA kreuzt unter den langsamen SMA.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**:
  - Trailing-Stop oder fester Stop-Loss.
  - Tägliches Gewinnziel schließt alle Positionen.
- **Stops**: Ja, fest und Trailing.
- **Standardwerte**:
  - `FastLength` = 9
  - `SlowLength` = 21
  - `DailyTarget` = 200
  - `StopLossAmount` = 100
  - `TrailOffset` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: SMA
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
