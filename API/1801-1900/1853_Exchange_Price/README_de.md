# Exchange Price Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie vergleicht den aktuellen Schlusskurs mit den Kursen mehrerer Balken zuvor über zwei Rückblickperioden. Eine Long-Position wird eröffnet, wenn die kurzfristige Änderung die langfristige Änderung übersteigt; eine Short-Position wird beim entgegengesetzten Crossover eröffnet.

## Details

- **Einstiegskriterien**: kurzfristige Preisdifferenz kreuzt langfristige Differenz nach oben/unten
- **Long/Short**: Beide
- **Ausstiegskriterien**: entgegengesetzter Crossover
- **Stops**: Nein
- **Standardwerte**:
  - `ShortPeriod` = 96
  - `LongPeriod` = 288
  - `CandleType` = 8-Stunden-Kerzen
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Preisdifferenz
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: 8 Stunden
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
