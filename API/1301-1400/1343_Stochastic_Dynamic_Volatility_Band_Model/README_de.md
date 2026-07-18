# Stochastic-Dynamic Volatility Band Model-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Verwendet Bollinger-artige Volatilitätsbänder, um Kreuzungspunkte zu handeln und nach einer festen Anzahl von Kerzen auszusteigen.

## Details

- **Einstiegskriterien**: Long, wenn der Preis das untere Band nach oben kreuzt; Short, wenn der Preis das obere Band nach unten kreuzt
- **Long/Short**: Beide
- **Ausstiegskriterien**: Position wird nach `ExitBars` Kerzen geschlossen
- **Stops**: Nein
- **Standardwerte**:
  - `Length` = 5
  - `Multiplier` = 1.67
  - `ExitBars` = 7
- **Filter**:
  - Kategorie: Volatilität
  - Richtung: Beide
  - Indikatoren: BollingerBands
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
