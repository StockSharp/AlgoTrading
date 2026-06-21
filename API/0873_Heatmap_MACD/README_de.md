# Heatmap MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Heatmap MACD handelt, wenn MACD-Histogramme aus fünf Zeitrahmen übereinstimmen. Eine Long-Position wird eröffnet, wenn alle Histogramme positiv werden, und eine Short-Position, wenn alle negativ werden. Optional kann die Position geschlossen werden, wenn ein Histogramm gegen den Trade dreht.

## Details
- **Daten**: Preiskerzen.
- **Einstiegskriterien**:
  - **Long**: MACD-Histogramm > 0 auf allen fünf Zeitrahmen und zuvor nicht alle positiv.
  - **Short**: MACD-Histogramm < 0 auf allen fünf Zeitrahmen und zuvor nicht alle negativ.
- **Ausstiegskriterien**: Entgegengesetztes Signal oder optionaler Schlusskurs bei Gegensignal.
- **Stops**: Standardmäßig keine.
- **Standardwerte**:
  - `FastLength` = 9
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `TimeFrame1` = tf(60)
  - `TimeFrame2` = tf(120)
  - `TimeFrame3` = tf(240)
  - `TimeFrame4` = tf(240)
  - `TimeFrame5` = tf(480)
  - `CloseOnOpposite` = false
- **Filter**:
  - Kategorie: Trend
  - Richtung: Long & Short
  - Indikatoren: MACD
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Multi-Zeitrahmen
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
