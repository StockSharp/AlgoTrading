# EUR/USD Mehrschichtige Statistische Regressions-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die mehrere lineare Regressionschichten verwendet, um die Trendrichtung bei EUR/USD zu schätzen. Sie berechnet kurze, mittlere und lange Regressionen, validiert diese durch R²- und Steigungsschwellenwerte und handelt in Richtung des gewichteten Ensembles.

## Details

- **Einstiegskriterien**:
  - Long: gewichtete Steigung > 0 und Zuverlässigkeit > 0.5
  - Short: gewichtete Steigung < 0 und Zuverlässigkeit > 0.5
- **Long/Short**: Beide
- **Ausstiegskriterien**: Umkehr bei entgegengesetztem Signal
- **Stops**: Tagesverlustschutz
- **Standardwerte**:
  - `ShortLength` = 20
  - `MediumLength` = 50
  - `LongLength` = 100
  - `MinRSquared` = 0.45m
  - `SlopeThreshold` = 0.00005m
  - `WeightShort` = 0.4m
  - `WeightMedium` = 0.35m
  - `WeightLong` = 0.25m
  - `PositionSizePct` = 50m
  - `MaxDailyLossPct` = 12m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Linear Regression
  - Stops: Ja
  - Komplexität: Fortgeschritten
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Risikolevel: Mittel
