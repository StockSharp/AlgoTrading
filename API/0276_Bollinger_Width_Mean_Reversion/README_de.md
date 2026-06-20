# Bollinger-Breiten-Mean-Reversion-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Bollinger-Breiten-Mean-Reversion-Strategie konzentriert sich auf extreme Ablesungen der Bollinger-Breite, um Rückkehrpotenziale zu nutzen. Starke Abweichungen vom Durchschnittsniveau halten selten an.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 157%. Die Strategie funktioniert am besten auf dem Kryptomarkt.

Trades werden ausgelöst, wenn der Indikator weit von seinem Mittelwert abweicht und dann beginnt, sich umzukehren. Sowohl Long- als auch Short-Setups beinhalten einen Schutz-Stop.

Geeignet für Swing-Trader, die Schwankungen erwarten. Die Strategie schließt Positionen, sobald die Bollinger-Breite wieder zum Gleichgewicht zurückkehrt. Ausgangsparameter `BollingerLength` = 20.

## Details

- **Einstiegskriterien**: Indikator kreuzt zurück zum Mittelwert.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Indikator kehrt zum Durchschnitt zurück.
- **Stops**: Ja.
- **Standardwerte**:
  - `BollingerLength` = 20
  - `BollingerDeviation` = 2.0m
  - `WidthLookbackPeriod` = 20
  - `WidthDeviationMultiplier` = 2.0m
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Bollinger
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
