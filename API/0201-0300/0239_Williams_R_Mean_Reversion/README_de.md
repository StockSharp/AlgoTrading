# Williams %R Mean Reversion-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Williams %R oszilliert zwischen 0 und -100, um anzuzeigen, wann der Preis nahe den Extremen seiner jüngsten Range schließt. Diese Strategie handelt gegen diese Extreme, sobald der Indikator weit von seinem eigenen Durchschnitt abweicht.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 154%. Er funktioniert am besten auf dem Aktienmarkt.

Ein Long-Trade wird ausgelöst, wenn Williams %R unter den Durchschnitt minus `DeviationMultiplier` mal die Standardabweichung fällt. Ein Short-Trade wird eingegangen, wenn er über den Durchschnitt plus diesen Multiplikator steigt. Ausstiege erfolgen, wenn Williams %R wieder zum Durchschnittsniveau zurückkehrt.

Der Ansatz eignet sich für Trader, die sich auf Momentumerschöpfung stützen, um Einstiege zu timen. Ein schützender Stop-Loss begrenzt das Risiko, wenn der Preis weiterhin neue Extreme erreicht.

## Details
- **Einstiegskriterien**:
  - **Long**: %R < Avg - DeviationMultiplier * StdDev
  - **Short**: %R > Avg + DeviationMultiplier * StdDev
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Ausstieg wenn %R > Avg
  - **Short**: Ausstieg wenn %R < Avg
- **Stops**: Ja, prozentualer Stop-Loss.
- **Standardwerte**:
  - `WilliamsRPeriod` = 14
  - `AveragePeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Williams %R
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

