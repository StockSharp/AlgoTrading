# Color Zerolag Momentum X2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Momentum-Strategie mit zwei Zeitrahmen, die eine Zero-Lag-Moving-Average-Kreuzung verwendet. Der höhere Zeitrahmen definiert die Trendrichtung, während der niedrigere Zeitrahmen Einstiege auslöst, wenn Momentum seine Zero-Lag-Average in Trendrichtung kreuzt.

## Details

- **Einstiegskriterien**: Momentum kreuzt seine Zero-Lag-Average in Trendrichtung
- **Long/Short**: Beide
- **Ausstiegskriterien**: entgegengesetzte Kreuzung oder Trendumkehr
- **Stops**: Nein
- **Standardwerte**:
  - `TrendCandleType` = 6h
  - `TrendMomentumPeriod` = 34
  - `TrendMaLength` = 15
  - `SignalCandleType` = 30m
  - `SignalMomentumPeriod` = 34
  - `SignalMaLength` = 15
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Momentum, ZeroLagEMA
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Multi-Zeitrahmen
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
