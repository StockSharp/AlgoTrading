# Vicious Mortgage Rates V1-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Handelt einen synthetischen Index, der aus vier Volatilitätsmaßen aufgebaut ist.
Eine Long-Position wird eröffnet, wenn der schnelle EMA des Produkts den langsamen EMA von unten kreuzt, eine Short-Position beim entgegengesetzten Kreuz.

## Details

- **Einstiegskriterien**: schneller EMA des kombinierten Index kreuzt den langsamen EMA nach oben
- **Long/Short**: Beide
- **Ausstiegskriterien**: entgegengesetztes Kreuz
- **Stops**: Nein
- **Standardwerte**:
  - `FastLength` = 8
  - `SlowLength` = 21
- **Filter**:
  - Kategorie: Volatilität
  - Richtung: Beide
  - Indikatoren: EMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
