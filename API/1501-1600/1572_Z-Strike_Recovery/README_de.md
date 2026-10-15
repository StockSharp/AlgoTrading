# Z-Strike Recovery-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Geht long, wenn der Z-Score der Preisänderung einen Schwellenwert überschreitet, und schließt die Position nach einer festen Anzahl von Bars.

## Details

- **Einstiegskriterien**: Z-Score der Preisänderung > Schwellenwert
- **Long/Short**: Nur Long
- **Ausstiegskriterien**: Zeitbasierter Ausstieg
- **Stops**: Nein
- **Standardwerte**:
  - `ZLength` = 16
  - `ZThreshold` = 1.3
  - `ExitPeriods` = 10
- **Filter**:
  - Kategorie: Statistisch
  - Richtung: Long
  - Indikatoren: SMA, StandardDeviation
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
