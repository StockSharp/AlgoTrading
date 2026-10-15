# RSI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Einfache Strategie basierend auf dem Relative Strength Index. Kauft, wenn der RSI den überverkauften Level nach oben kreuzt, und verkauft, wenn er den überkauften Level nach unten kreuzt.

## Details

- **Einstiegskriterien**:
  - Long: RSI kreuzt `OverSold` nach oben
  - Short: RSI kreuzt `OverBought` nach unten
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Gegensätzliches Signal
- **Stops**: Nein
- **Standardwerte**:
  - `RsiLength` = 14
  - `OverSold` = 25m
  - `OverBought` = 75m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Beide
  - Indikatoren: RSI
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
