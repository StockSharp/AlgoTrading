# Multi-EMA-Crossover-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie eröffnet separate Long-Positionen für vier EMA-Paare, wenn der schnellere EMA den langsameren nach oben kreuzt. Jede Position schließt, wenn ihr schneller EMA unter den langsameren EMA fällt.

## Details

- **Einstiegskriterien**:
  - **Long**: Schneller EMA kreuzt über den langsamen EMA für eines der Paare (1/5, 3/10, 5/20, 10/40).
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - Schneller EMA fällt unter den langsamen EMA für das jeweilige Paar.
- **Stops**: Keine.
- **Standardwerte**:
  - `EMA1` = 1
  - `EMA3` = 3
  - `EMA5` = 5
  - `EMA10` = 10
  - `EMA20` = 20
  - `EMA40` = 40
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Nur Long
  - Indikatoren: EMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
