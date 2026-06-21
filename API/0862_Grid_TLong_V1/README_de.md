# Grid TLong V1-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Grid-basierte Strategie, die kontinuierlich eine Position hält. Sie tritt erneut in Positionen ein, wenn der Gewinn oder Verlust einen festen prozentualen Schritt erreicht.

## Details

- **Einstiegskriterien**: Immer im Markt; Positionen an Grid-Schritten neu eröffnen.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal oder erneuter Einstieg nach Erreichen des Prozentschritts.
- **Stops**: Nein.
- **Standardwerte**:
  - `Percent` = 1
  - `UseLimitOrders` = false
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Grid
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
