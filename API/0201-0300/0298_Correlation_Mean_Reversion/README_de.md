# Korrelations-Mean-Reversion-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Korrelations-Mean-Reversion-Strategie konzentriert sich auf extreme Werte der Correlation, um Mean Reversion auszunutzen. Starke Abweichungen vom typischen Niveau halten selten an.

Trades werden ausgelöst, wenn der Indikator weit von seinem Mittelwert abweicht und dann beginnt, sich umzukehren. Sowohl Long- als auch Short-Setups umfassen einen Schutz-Stop.

Geeignet für Swing-Trader, die Oszillationen erwarten; die Strategie schließt die Position, sobald die Correlation wieder in Richtung Gleichgewicht zurückkehrt. Startparameter `CorrelationPeriod` = 20.

## Details

- **Einstiegskriterien**: Der Indikator kreuzt zurück in Richtung Mittelwert.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Der Indikator kehrt zum Durchschnitt zurück.
- **Stops**: Ja.
- **Standardwerte**:
  - `CorrelationPeriod` = 20
  - `LookbackPeriod` = 20
  - `DeviationThreshold` = 2.0m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Correlation
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
