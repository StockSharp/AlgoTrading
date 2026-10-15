# Preis-Statistischer-ZScore-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie mit geglättetem Z-Score-Crossover und einem Kerzen-Momentum-Filter.

Kauft, wenn der kurzfristige Z-Score über den langfristigen Z-Score steigt, und schließt, wenn er darunter fällt. Die Strategie ignoriert Signale nach mehreren identischen und vermeidet Einstiege nach drei aufeinanderfolgenden bullischen Kerzen.

## Details

- **Einstiegskriterien**: Kurzfristiger Z-Score über dem langfristigen, keine vorangegangene bullische 3-Balken-Sequenz, Abstand zwischen Signalen.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Kurzfristiger Z-Score unter dem langfristigen, keine vorangegangene bärische 3-Balken-Sequenz, Abstand zwischen Signalen.
- **Stops**: Nein.
- **Standardwerte**:
  - `ZScoreBasePeriod` = 3
  - `ShortSmoothPeriod` = 3
  - `LongSmoothPeriod` = 5
  - `GapBars` = 5
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Nur Long
  - Indikatoren: SMA, StandardDeviation
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
