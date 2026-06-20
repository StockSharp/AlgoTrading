# Javo v1-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Javo v1 kombiniert Heikin Ashi-Kerzen mit einem Paar exponentieller gleitender Durchschnitte. Eine Position wird eröffnet, wenn die HA-Richtung und der Crossover des schnellen/langsamen EMA übereinstimmen. Der Ansatz versucht, aufkommende Trends zu erfassen und dabei Rauschen zu glätten.

## Details

- **Einstiegskriterien**:
  - **Long**: HA bullish und `EMA_fast > EMA_slow`
  - **Short**: HA bearish und `EMA_fast < EMA_slow`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Entgegengesetztes Signal
- **Stops**: Keine
- **Standardwerte**:
  - `FastEmaPeriod` = 1
  - `SlowEmaPeriod` = 30
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Heikin Ashi, EMA
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Stündlich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
