# Zero Lag MACD + Kijun-sen + EOM-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die Zero Lag MACD mit der Kijun-sen-Basislinie und dem Ease of Movement-Filter kombiniert. Verwendet ATR-basierten Stop und Take Profit.

## Details

- **Einstiegskriterien**: MACD-Kreuzung mit Kijun-sen- und EOM-Filter.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: ATR-basierter Stop oder Take Profit.
- **Stops**: Ja.
- **Standardwerte**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `MacdEmaLength` = 9
  - `KijunPeriod` = 26
  - `EomLength` = 14
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.5m
  - `RiskReward` = 1.2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: MACD, Donchian, EOM, ATR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
