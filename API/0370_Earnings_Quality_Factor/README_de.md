# Earnings-Quality-Factor-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Earnings Quality Factor**-Strategie rebalanciert jährlich am 1. Juli, geht Long in Aktien hoher Qualität und Short in Aktien niedriger Qualität basierend auf Earnings-Quality-Scores.

## Details
- **Einstiegskriterien**: Jährliches Rebalancing am 1. Juli anhand von Qualitäts-Scores.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Nächstes jährliches Rebalancing.
- **Stops**: Nein.
- **Standardwerte**:
  - `MinTradeUsd = 100`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Fundamental
  - Richtung: Beide
  - Indikatoren: Qualität
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Täglich
  - Saisonalität: Ja
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
