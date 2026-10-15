# MA-Steigungs-Mean-Reversion-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die MA-Steigungs-Mean-Reversion-Strategie konzentriert sich auf extreme Ablesungen des MA-Indikators, um Rückkehrpotenziale zu nutzen. Starke Abweichungen vom jüngsten Niveau halten selten an.

Trades werden ausgelöst, wenn der Indikator weit von seinem Mittelwert abweicht und dann beginnt, sich umzukehren. Sowohl Long- als auch Short-Setups beinhalten einen Schutz-Stop.

Geeignet für Swing-Trader, die Schwankungen erwarten. Die Strategie schließt Positionen, sobald der MA wieder zum Gleichgewicht zurückkehrt. Ausgangsparameter `MaPeriod` = 20.

## Details

- **Einstiegskriterien**: Indikator kreuzt zurück zum Mittelwert.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Indikator kehrt zum Durchschnitt zurück.
- **Stops**: Ja.
- **Standardwerte**:
  - `MaPeriod` = 20
  - `SlopeLookback` = 20
  - `ThresholdMultiplier` = 2m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: MA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
