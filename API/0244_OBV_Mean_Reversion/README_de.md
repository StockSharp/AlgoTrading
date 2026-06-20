# OBV Mean-Reversion-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der On Balance Volume (OBV) verfolgt den kumulierten Volumenfluss, um zu bestimmen, ob Käufer oder Verkäufer dominant sind. Diese Strategie wartet darauf, dass der OBV stark von seinem Durchschnitt abweicht, und handelt dann in Erwartung einer Rückkehr zu typischen Niveaus.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 79%. Die Strategie funktioniert am besten am Aktienmarkt.

Ein Kaufsignal tritt auf, wenn der OBV unter seinen Durchschnitt minus `Multiplier` mal die Standardabweichung fällt und der Preis unter dem gleitenden Durchschnitt liegt. Ein Verkaufssignal wird erzeugt, wenn der OBV über das obere Band steigt und der Preis über dem Durchschnitt liegt. Positionen schließen, wenn der OBV zurück durch seine mittlere Linie kreuzt.

Der Ansatz ist für Trader nützlich, die neben der Preisbewegung auch Volumenflüsse berücksichtigen. Stops werden in einem festgelegten Prozentsatz platziert, um Situationen zu bewältigen, in denen das Volumen weiter beschleunigt.

## Details
- **Einstiegskriterien**:
  - **Long**: OBV < Avg - Multiplier * StdDev && Close < MA
  - **Short**: OBV > Avg + Multiplier * StdDev && Close > MA
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Ausstieg wenn OBV > Avg
  - **Short**: Ausstieg wenn OBV < Avg
- **Stops**: Ja, prozentualer Stop-Loss.
- **Standardwerte**:
  - `AveragePeriod` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: OBV
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
