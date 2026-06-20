# Fibonacci-Retracement Umkehrstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Märkte retracieren häufig einen Teil einer vorherigen Bewegung, bevor sie den Trend fortsetzen. Diese Strategie identifiziert jüngste Swing-Hochs und -Tiefs und beobachtet, wie der Kurs die Retracement-Niveaus von 61.8% oder 78.6% testet. Diese Bereiche markieren häufig Erschöpfungspunkte.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 115%. Die Strategie eignet sich am besten für den Aktienmarkt.

Der Algorithmus verfolgt Swings über ein rollendes Fenster und berechnet Fibonacci-Niveaus zwischen ihnen. Wenn der Kurs sich einem wichtigen Retracement nähert und eine Kerze in Richtung des ursprünglichen Trends bildet, wird ein Trade mit einem in festem Prozentabstand platzierten Stop eröffnet. Ziele liegen um den 50%-Mittelpunkt des Swings.

Indem der Fokus auf tiefen Pullbacks innerhalb eines bestehenden Trends liegt, zielt die Methode darauf ab, die frühen Phasen einer Fortsetzungsbewegung zu erfassen, nachdem Verkäufer oder Käufer kurzzeitig die Kontrolle übernommen haben.

## Details

- **Einstiegskriterien**: Kurs testet 61.8%- oder 78.6%-Retracement und bildet eine bestätigende Kerze.
- **Long/Short**: Beide abhängig vom Trend.
- **Ausstiegskriterien**: Kurs erreicht das 50%-Niveau oder Stop-Loss.
- **Stops**: Ja, prozentbasiert.
- **Standardwerte**:
  - `SwingLookbackPeriod` = 20
  - `FibLevelBuffer` = 0.5
  - `CandleType` = 5 minute
  - `StopLossPercent` = 2
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Fibonacci levels
  - Stops: Ja
  - Komplexität: Fortgeschritten
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

