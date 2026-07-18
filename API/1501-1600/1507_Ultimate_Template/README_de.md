# Ultimate-Strategie-Vorlage
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Grundlegende Moving-Average-Crossover-Vorlage, die Long- oder Short-Positionen eröffnet, wenn sich schnelle und langsame Durchschnitte kreuzen. Enthält optionale prozentuale Stop-Loss- und Take-Profit-Schutzfunktionen.

## Details

- **Einstiegskriterien**: Schneller SMA kreuzt den langsamen SMA.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Gegenseitige Kreuzung oder Risikoabsicherungen.
- **Stops**: Prozentualer Stop-Loss und Take-Profit.
- **Standardwerte**:
  - `FastLength` = 9
  - `SlowLength` = 21
  - `StopLossPercent` = 1
  - `TakeProfitPercent` = 3
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: SMA
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Mittel
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
