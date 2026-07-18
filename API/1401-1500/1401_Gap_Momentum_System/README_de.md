# Gap-Momentum-System-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementiert das Gap-Momentum-System von Perry Kaufman. Die Strategie vergleicht akkumulierte Aufwärts- und Abwärtsgaps und handelt, wenn das Signal steigt oder fällt.

## Details
- **Einstiegskriterien**: Steigendes Signal -> kaufen, fallendes Signal -> verkaufen oder umkehren.
- **Long/Short**: Konfigurierbar.
- **Ausstiegskriterien**: Entgegengesetztes Signal.
- **Stops**: Keine.
- **Standardwerte**:
  - `Period` = 40
  - `SignalPeriod` = 20
  - `LongOnly` = true
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide oder nur Long
  - Indikatoren: Gap momentum
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
