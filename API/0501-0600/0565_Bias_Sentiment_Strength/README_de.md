# Bias- und Sentiment-Stärke-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie aggregiert mehrere Momentum- und Volumenindikatoren (MACD, RSI, Stochastic, Awesome Oscillator, Alligator-Durchschnitte und Volumen-Bias) zu einem einzelnen Bias-Wert. Eine Long-Position wird eröffnet, wenn der kombinierte Bias über null liegt, und eine Short-Position, wenn er unter null liegt.

## Details

- **Einstiegskriterien**:
  - **Long**: Kombinierter Bias > 0.
  - **Short**: Kombinierter Bias < 0.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Umgekehrtes Signal.
- **Stops**: Stop-Loss-Prozentsatz über `StopLossPercent`.
- **Standardwerte**:
  - MACD schnell 12, langsam 26, Signal 9.
  - RSI-Periode 14.
  - Stochastic-Perioden 21/14/14.
  - Awesome Oscillator-Perioden 5/34.
  - Volumen-Bias-Länge 30.
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Mehrere
  - Stops: Ja
  - Komplexität: Komplex
  - Zeitrahmen: Mittelfristig
