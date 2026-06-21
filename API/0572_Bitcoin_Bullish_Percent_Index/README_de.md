# Bitcoin Bullish Percent Index-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet den Relative Strength Index (RSI), um den Bitcoin Bullish Percent Index anzunähern. Sie geht Long, wenn der RSI über das überverkaufte Niveau steigt, und Short, wenn der RSI unter das überkaufte Niveau fällt.

## Details

- **Einstiegskriterien**:
  - **Long**: RSI kreuzt über das überverkaufte Niveau.
  - **Short**: RSI kreuzt unter das überkaufte Niveau.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Gegensätzliches Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `RSI Period` = 14
  - `Overbought` = 70
  - `Oversold` = 30
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Beide
  - Indikatoren: RSI
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Mittelfristig
