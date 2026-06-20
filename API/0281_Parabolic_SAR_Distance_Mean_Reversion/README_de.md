# Parabolic SAR-Distanz-Mean-Reversion-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Parabolic SAR-Distanz-Mean-Reversion-Strategie konzentriert sich auf extreme Ablesungen des Parabolic SAR-Indikators, um Rückkehrpotenziale zu nutzen. Starke Abweichungen vom normalen Niveau halten selten an.

Trades werden ausgelöst, wenn der Indikator weit von seinem Mittelwert abweicht und dann beginnt, sich umzukehren. Sowohl Long- als auch Short-Setups beinhalten einen Schutz-Stop.

Geeignet für Swing-Trader, die Schwankungen erwarten. Die Strategie schließt Positionen, sobald der Parabolic SAR wieder zum Gleichgewicht zurückkehrt. Ausgangsparameter `AccelerationFactor` = 0.02m.

## Details

- **Einstiegskriterien**: Indikator kreuzt zurück zum Mittelwert.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Indikator kehrt zum Durchschnitt zurück.
- **Stops**: Ja.
- **Standardwerte**:
  - `AccelerationFactor` = 0.02m
  - `AccelerationLimit` = 0.2m
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Parabolic SAR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
