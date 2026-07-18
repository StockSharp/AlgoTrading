# PercentX Trendfolge-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie abgeleitet von PercentX Trend Follower von Trendoscope.

Die Strategie normalisiert den Preisabstand von einem ausgewählten Band (Keltner oder Bollinger) und handelt, wenn dieser Oszillator dynamische Extrembereiche kreuzt. ATR wird für die Stop-Platzierung verwendet.

## Details

- **Einstiegskriterien**: Oszillator kreuzt oberhalb des oberen Bereichs für Long, unterhalb des unteren Bereichs für Short.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: ATR-basierter Stop.
- **Stops**: Initialer ATR-Stop.
- **Standardwerte**:
  - `BandType` = Keltner
  - `MaLength` = 40
  - `LoopbackPeriod` = 80
  - `OuterLoopback` = 80
  - `UseInitialStop` = true
  - `AtrLength` = 14
  - `TrendMultiplier` = 1
  - `ReverseMultiplier` = 3
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: BollingerBands, KeltnerChannels, ATR, Highest, Lowest
  - Stops: ATR
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
