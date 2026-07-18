# Simple Bars-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Simple Bars-Strategie repliziert das Verhalten des ursprünglichen MQL5-Experten `Exp_SimpleBars`. Sie verwendet den *SimpleBars*-Indikator, um den aktuellen Trend zu bestimmen, indem die letzte Kerze mit den letzten Hochs und Tiefs verglichen wird. Wenn der Indikator eine Trendänderung erkennt, führt die Strategie einen Trade am Eröffnungskurs der nächsten Bar aus.

## Details

- **Einstiegskriterien**
  - **Long**: Das Indikatorsignal der vorherigen Bar ist *buy*.
  - **Short**: Das Indikatorsignal der vorherigen Bar ist *sell*.
- **Long/Short**: Beide Richtungen werden gehandelt.
- **Ausstiegskriterien**
  - Die Position wird umgekehrt, wenn ein entgegengesetztes Signal erscheint.
- **Stops**: Keine.
- **Standardwerte**
  - `Period` = 6 Bars.
  - `UseClose` = `true` (Schlusskurs wird für den Vergleich verwendet).
  - `CandleType` = 6-Stunden-Kerzen.
- **Filter**
  - Kategorie: Trendfolge.
  - Richtung: Beide.
  - Indikatoren: Benutzerdefiniert.
  - Stops: Nein.
  - Komplexität: Moderat.
  - Zeitrahmen: Mittelfristig.
  - Saisonalität: Nein.
  - Neuronale Netze: Nein.
  - Divergenz: Nein.
  - Risikolevel: Mittel.
