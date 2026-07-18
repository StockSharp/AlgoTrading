# Volatilitäts-Bias-Modell-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Zählt bullische gegenüber bärischen Schlusskursen über ein Fenster und handelt in Richtung des dominanten Bias, wenn die Volatilität ausreichend ist. Verwendet ATR-Ziele und schließt die Position nach einer maximalen Anzahl von Bars.

## Details
- **Einstiegskriterien**: Bias-Verhältnis über `BiasThreshold` für Long oder unter `1 - BiasThreshold` für Short, wenn die Range über `RangeMin` liegt.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Stop, Take-Profit oder `MaxBars` erreicht.
- **Stops**: Ja.
- **Standardwerte**:
  - `BiasWindow` = 10
  - `BiasThreshold` = 0.6
  - `RangeMin` = 0.05
  - `RiskReward` = 2
  - `MaxBars` = 20
  - `AtrLength` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Volatilität
  - Richtung: Beide
  - Indikatoren: ATR, SMA, Highest, Lowest
  - Stops: Ja
  - Komplexität: Anfänger
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
