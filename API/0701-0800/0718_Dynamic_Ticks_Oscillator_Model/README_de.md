# Dynamisches Tick-Oszillator-Modell (DTOM)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das **Dynamic Ticks Oscillator Model** nutzt die Veränderungsrate des NYSE Down Ticks Index. Wenn der ROC unter einen dynamischen Schwellenwert auf Basis der Standardabweichung fällt, eröffnet die Strategie eine Long-Position. Die Position wird geschlossen, sobald der ROC über einen positiven Schwellenwert steigt.

## Details
- **Einstiegskriterien**: `ROC < -StdDev * EntryStdDevMultiplier`
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: `ROC > StdDev * ExitStdDevMultiplier`
- **Stops**: Nein.
- **Standardwerte**:
  - `RocLength = 5`
  - `VolatilityLookback = 24`
  - `EntryStdDevMultiplier = 1.6m`
  - `ExitStdDevMultiplier = 1.4m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Nur Long
  - Indikatoren: RateOfChange, StandardDeviation
  - Stops: Nein
  - Komplexität: Anfänger
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
