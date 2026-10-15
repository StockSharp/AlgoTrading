# RedK Compound-Ratio-MA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Handelt Long, wenn der gleitende Durchschnitt mit zusammengesetztem Verhältnis (CoRa Wave) steigt, und Short, wenn er fällt.

## Details

- **Einstiegskriterien**:
  - Long: CoRa-Wave-Wert steigt über den vorherigen Wert
  - Short: CoRa-Wave-Wert fällt unter den vorherigen Wert
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Gegenläufiges Signal
- **Stops**: Keine
- **Standardwerte**:
  - `Length` = 20
  - `RatioMultiplier` = 2m
  - `AutoSmoothing` = true
  - `ManualSmoothing` = 1
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Compound Ratio MA, Weighted Moving Average
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Keine
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
