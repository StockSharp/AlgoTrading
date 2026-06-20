# RSI 30-70-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese einfache Momentum-Strategie verwendet den Relative Strength Index (RSI), um überverkaufte und überkaufte Zonen zu identifizieren. Wenn der RSI unter den Überverkauft-Level fällt, wird eine Long-Position eröffnet. Der Trade wird geschlossen, sobald der RSI über den Überkauft-Schwellenwert steigt. Das System operiert auf einem einzigen Zeitrahmen und geht keine Short-Trades ein.

## Details

- **Einstiegskriterien**:
  - **Long**: `RSI < oversold`.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - **Long**: `RSI > overbought`.
- **Stops**: Keine.
- **Standardwerte**:
  - `RSI Length` = 14.
  - `Overbought/Oversold` = 70 / 30.
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Long
  - Indikatoren: Einzeln
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
