# ColorXvaMA Digit-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt auf Basis der Steigungsänderung eines doppelt geglätteten gleitenden Durchschnitts. Ein Exponentieller Gleitender Durchschnitt wird erneut durch einen Jurik Moving Average geglättet. Eine Long-Position wird eröffnet, wenn der schnelle JMA den langsamen EMA von unten nach oben kreuzt, eine Short-Position, wenn er von oben nach unten kreuzt.

## Details

- **Einstiegskriterien**:
  - **Long**: Schneller JMA kreuzt langsamen EMA nach oben.
  - **Short**: Schneller JMA kreuzt langsamen EMA nach unten.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**: Gegensätzliches Signal.
- **Stops**: Keine.
- **Standardwerte**:
  - `SlowLength` = 15
  - `FastLength` = 5
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: EMA, JMA
  - Stops: Keine
  - Komplexität: Niedrig
  - Zeitrahmen: 8h
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
