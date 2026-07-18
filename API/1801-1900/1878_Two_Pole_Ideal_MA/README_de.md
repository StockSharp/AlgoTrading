# Two-Pole Ideal MA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Crossover-System, das den Experten „2pb Ideal MA" annähert, indem es einen schnellen EMA mit einem langsamen TEMA vergleicht.

## Details

- **Einstiegskriterien**: Schneller EMA kreuzt langsamen TEMA.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Umkehr beim entgegengesetzten Crossover.
- **Stops**: Nein.
- **Standardwerte**:
  - `FastPeriod` = 10
  - `SlowPeriod` = 30
  - `CandleType` = TimeSpan.FromHours(4)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: EMA, TEMA
  - Stops: Nein
  - Komplexität: Anfänger
  - Zeitrahmen: Swing (H4)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
