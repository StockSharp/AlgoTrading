# VIDYA ProTrend Multi-Stufen-Gewinn-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Trendfolge-Strategie, die schnelle und langsame VIDYA-Durchschnitte mit einem Bollinger-Band-Filter verwendet.
Optional werden mehrstufige Take-Profit-Orders mit ATR-Vielfachen und prozentualen Zielen platziert.

## Details

- **Einstiegskriterien**: schnelles VIDYA über langsamem VIDYA mit Preis außerhalb des Bollinger-Filters
- **Long/Short**: Beide
- **Ausstiegskriterien**: entgegengesetzte Steigung oder Kreuz
- **Stops**: Nein
- **Standardwerte**:
  - `FastVidyaLength` = 10
  - `SlowVidyaLength` = 30
  - `MinSlopeThreshold` = 0.05
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: VIDYA, Bollinger Bands, ATR
  - Stops: Nein
  - Komplexität: Fortgeschritten
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
