# Hancock RSI Volumen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie berechnet einen volumengewichteten Relative Strength Index (RSI), inspiriert vom Hancock-Skript auf TradingView. Der RSI nutzt bullisches und bärisches Volumen, um die Trendstärke zu messen. Eine Long-Position wird eröffnet, wenn der RSI-Trend nach oben dreht, und eine Short-Position, wenn er nach unten dreht.

## Details

- **Einstiegskriterien**:
  - **Long**: RSI-Trend wechselt nach oben.
  - **Short**: RSI-Trend wechselt nach unten.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Entgegengesetztes Trendsignal.
- **Stops**: Keine.
- **Standardwerte**:
  - `RSI Length` = 14.
  - `Threshold` = 0.1.
  - `Use Wicks` = true.
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: RSI, Volumen
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
