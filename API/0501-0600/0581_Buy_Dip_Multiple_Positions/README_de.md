# Strategie zum Kauf von Kursrücksetzern mit mehreren Positionen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Buy Dip Multiple Positions-Strategie fügt Long-Positionen hinzu, wenn ein Kursrücksetzer zusammen mit hohem Volumen und einer Preisschub-Bedingung auftritt. Jeder Trade riskiert 2% des Eigenkapitals und teilt gemeinsame Trailing-Stop- und Zielniveaus. Eine neue Position wird nur eröffnet, wenn der vorherige abgeschlossene Trade profitabel war.

## Details

- **Einstiegskriterien**:
  - Schlusskurs liegt 0,2% unter dem vorherigen Tief.
  - Volumen über 120% des Durchschnitts der letzten zwei Bars.
  - Schlusskurs unter dem Schlusskurs vor N Bars multipliziert mit `PriceSurgePercent` / 100.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - Anfangsstop als Prozentsatz des Tiefs der Einstiegsbar.
  - Trailing-Stop, der nach dem Setup mit jeder Bar steigt.
  - Zielkurs oberhalb des Tiefs der Einstiegsbar.
- **Stops**: Ja.
- **Standardwerte**:
  - `MaxPositions` = 20
  - `TrailRatePercent` = 1
  - `InitialStopPercent` = 85
  - `TargetPricePercent` = 60
  - `PriceSurgePercent` = 89
  - `SurgeLookbackBars` = 14
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Long
  - Indikatoren: Volumen, Kursverhalten
  - Stops: Ja
  - Komplexität: Moderat
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
