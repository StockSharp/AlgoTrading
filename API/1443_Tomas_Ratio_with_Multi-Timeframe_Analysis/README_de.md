# Tomas-Ratio-Strategie mit Multi-Zeitrahmen-Analyse
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie akkumuliert gewichtete Gewinne und Verluste über mehrere Zeitrahmen, um ein Tomas-Ratio-Signal zu bilden. Trades werden eröffnet, wenn die Signalstärke ein Ziel überschreitet, und geschlossen, wenn Schwäche dominiert.

## Details

- **Einstiegskriterien**: Signalstärke überschreitet Ziel und Preis über EMA(720)
- **Long/Short**: Nur Long
- **Ausstiegskriterien**: Schlusspunkte übersteigen Kaufpunkte
- **Stops**: Nein
- **Standardwerte**:
  - `CandleType` = 1-Stunden-Kerzen
  - `Length` = 720
  - `DeviationLength` = 168
  - `PointsTarget` = 100
  - `UseStandardDeviation` = true
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Nur Long
  - Indikatoren: Standard Deviation, SMA, EMA
  - Stops: Nein
  - Komplexität: Fortgeschritten
  - Zeitrahmen: Mehrere
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Hoch
