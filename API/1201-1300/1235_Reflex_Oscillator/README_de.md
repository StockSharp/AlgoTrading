# Reflex Oszillator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet John Ehlers' Reflex Oscillator. Sie geht long, wenn der Oszillator über eine obere Schwelle kreuzt, und short, wenn er unter eine untere Schwelle kreuzt. Positionen werden geschlossen, wenn der Oszillator zur Nulllinie zurückkehrt.

## Details

- **Einstiegskriterien**:
  - **Long**: Oszillator kreuzt über `UpperLevel`.
  - **Short**: Oszillator kreuzt unter `LowerLevel`.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Long-Position: Oszillator kreuzt unter null.
  - Short-Position: Oszillator kreuzt über null.
- **Stops**: Nein.
- **Standardwerte**:
  - `ReflexPeriod` = 20.
  - `SuperSmootherPeriod` = 8.
  - `PostSmoothPeriod` = 33.
  - `UpperLevel` = 1.
  - `LowerLevel` = -1.
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Beide
  - Indikatoren: Einzeln
  - Stops: Nein
  - Komplexität: Fortgeschritten
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
