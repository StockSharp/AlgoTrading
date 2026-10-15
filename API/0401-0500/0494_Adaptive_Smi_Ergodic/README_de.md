# Adaptive SMI-Ergodic-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die adaptive SMI-Ergodic-Strategie verwendet den True Strength Index (TSI)-Oszillator mit einer EMA-Signallinie, um Umkehrungen aus Überkauf- oder Überverkauft-Extremen zu erkennen. Eine Long-Position wird eröffnet, wenn TSI den Überverkauft-Schwellenwert nach oben kreuzt und dabei über seiner Signallinie bleibt. Eine Short-Position wird eröffnet, wenn TSI den Überkauf-Schwellenwert nach unten kreuzt und unter der Signallinie liegt.

## Details

- **Einstiegskriterien**:
  - TSI kreuzt über Überverkauft und TSI > Signal (Long).
  - TSI kreuzt unter Überkauft und TSI < Signal (Short).
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Ein umgekehrtes Signal löst den entgegengesetzten Trade aus.
- **Stops**: Keine.
- **Standardwerte**:
  - `LongLength` = 12
  - `ShortLength` = 5
  - `SignalLength` = 5
  - `OversoldThreshold` = -0.4
  - `OverboughtThreshold` = 0.4
- **Filter**:
  - Kategorie: Momentum-Oszillator
  - Richtung: Long/Short
  - Indikatoren: True Strength Index, EMA
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
