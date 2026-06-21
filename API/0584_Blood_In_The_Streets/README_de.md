# Blut-auf-den-Straßen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie kauft, wenn der aktuelle Drawdown vom jüngsten Höchststand unter einen Standardabweichungsschwellenwert fällt. Die Position wird nach einer festen Anzahl von Bars geschlossen.

## Details

- **Einstiegskriterien**:
  - Long: Drawdown ≤ Mittelwert + `StdDevThreshold` × Standardabweichung
- **Long/Short**: Nur Long
- **Ausstiegskriterien**: Position wird nach `ExitBars` Bars geschlossen
- **Stops**: Keine
- **Standardwerte**:
  - `LookbackPeriod` = 50
  - `StdDevLength` = 50
  - `StdDevThreshold` = -1m
  - `ExitBars` = 35
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Long
  - Indikatoren: Highest, SMA, StandardDeviation
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
