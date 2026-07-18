# Liquid-Pulse-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Erkennt hohe Volumenspitzen, die durch MACD und ADX bestätigt werden. ATR definiert Stop und Take-Profit mit täglichem Handelslimit.

## Details

- **Einstiegskriterien**:
  - Long: Volumenspitze, MACD kreuzt über Signal, +DI > -DI, ADX >= Schwellenwert
  - Short: Volumenspitze, MACD kreuzt unter Signal, -DI > +DI, ADX >= Schwellenwert
- **Long/Short**: Beide
- **Ausstiegskriterien**: ATR-basierter Stop oder Take-Profit
- **Stops**: ATR-Vielfache
- **Standardwerte**:
  - `VolumeSensitivity` = Medium
  - `MacdSpeed` = Medium
  - `DailyTradeLimit` = 20
  - `AtrPeriod` = 9
  - `AdxTrendThreshold` = 41
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: MACD, ADX, ATR, Volumen
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
