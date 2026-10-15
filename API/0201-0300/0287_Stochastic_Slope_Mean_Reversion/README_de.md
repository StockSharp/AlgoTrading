# Stochastic Slope Mean Reversion-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Die Stochastic Slope Mean Reversion-Strategie konzentriert sich auf extreme Stochastic-Werte, um Rückkehrbewegungen auszunutzen. Starke Abweichungen vom normalen Niveau halten selten lange an.

Trades werden ausgelöst, wenn der Indikator weit von seinem Mittelwert abweicht und dann beginnt, sich umzukehren. Sowohl Long- als auch Short-Setups enthalten einen Schutz-Stop.

Geeignet für Swing-Trader, die Oszillationen erwarten; die Strategie schließt die Position, sobald der Stochastic zum Gleichgewicht zurückkehrt. Startparameter `StochPeriod` = 14.

## Details

- **Einstiegskriterien**: Der Indikator kreuzt zurück in Richtung Mittelwert.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Der Indikator kehrt zum Durchschnitt zurück.
- **Stops**: Ja.
- **Standardwerte**:
  - `StochPeriod` = 14
  - `StochKPeriod` = 3
  - `StochDPeriod` = 3
  - `SlopeLookback` = 20
  - `ThresholdMultiplier` = 2m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Stochastic
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
