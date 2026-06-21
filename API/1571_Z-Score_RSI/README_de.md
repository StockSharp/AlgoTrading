# Z-Score RSI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Z-Score RSI-Strategie berechnet den RSI auf dem Z-Score des Preises und verwendet eine EMA des RSI für Signale. Eine Long-Position wird eröffnet, wenn der RSI seine EMA nach oben kreuzt, und eine Short-Position, wenn er sie nach unten kreuzt.

## Details

- **Einstiegskriterien**: RSI des Z-Scores kreuzt seine EMA
- **Long/Short**: Beide
- **Ausstiegskriterien**: Gegenläufige Kreuzung
- **Stops**: Nein
- **Standardwerte**:
  - `ZScoreLength` = 20
  - `RsiLength` = 9
  - `SmoothingLength` = 15
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Beide
  - Indikatoren: SMA, StandardDeviation, RSI, EMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
