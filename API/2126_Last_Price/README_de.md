# Letzter-Preis-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie platziert Limitaufträge beim besten Geld- oder Briefkurs, wenn sich der letzte Handelskurs um ein benutzerdefiniertes Intervall entfernt. Sie überwacht Level1-Orderbuch-Aktualisierungen und Handelsmeldungen, um Einstiege zu bestimmen.

## Details

- **Einstiegskriterien**:
  - **Long**: Letzter Preis ≥ bester Ask + Intervall.
  - **Short**: Letzter Preis ≤ bester Bid - Intervall.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**:
  - Gegenteiliges Signal oder außerhalb erlaubter Handelssitzungen.
- **Stops**: Nur Stop-Loss.
- **Standardwerte**:
  - `Interval` = 400
  - `Min Volume` = 1
  - `Max Volume` = 900000
  - `Spread` = 200
  - `Volume` = 1
  - `Stop Loss` = 400
- **Handelssitzungen**:
  - 10:05:40 – 13:54:30
  - 14:08:30 – 15:44:30
  - 16:05:30 – 18:39:30
  - 19:15:10 – 23:44:30
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
