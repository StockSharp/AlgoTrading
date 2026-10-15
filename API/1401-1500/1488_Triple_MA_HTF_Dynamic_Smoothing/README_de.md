# Triple MA HTF-Strategie - Dynamische Glättung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die drei gleitende Durchschnitte vergleicht, die auf höheren Zeitrahmen berechnet werden.
Jeder höhere Zeitrahmen-MA wird proportional zum Verhältnis zwischen seinem Zeitrahmen und dem Arbeitszeitrahmen geglättet.
Signale werden generiert, wenn der erste MA den zweiten kreuzt, während der dritte die Richtung bestätigt.

## Details

- **Einstiegskriterien**: Kreuzung von MA1 und MA2 mit MA3-Trendbestätigung.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `HigherTimeFrame1` = TimeSpan.FromMinutes(15)
  - `HigherTimeFrame2` = TimeSpan.FromMinutes(60)
  - `HigherTimeFrame3` = TimeSpan.FromMinutes(240)
  - `Length1` = 21
  - `Length2` = 21
  - `Length3` = 50
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: MA
  - Stops: Keine
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (Basis 5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
