# Trendabweichungs-Strategie BTC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kombiniert DMI-Kreuzungen mit Bollinger Bands und Bestätigungen von Momentum, MACD, SuperTrend und Aroon. Die Strategie sucht nach Preisabweichungen innerhalb eines Trends und steigt ein, wenn mehrere Signale übereinstimmen.

## Details

- **Einstiegskriterien**: +DI kreuzt über -DI, Preis unterhalb des oberen Bollinger Bands und eine beliebige Momentum/MACD/SuperTrend/Aroon-Bestätigung.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `DmiPeriod` = 15
  - `BbLength` = 13
  - `BbMultiplier` = 2.3
  - `MomentumLength` = 10
  - `AroonLength` = 5
  - `MacdFast` = 15
  - `MacdSlow` = 200
  - `MacdSignal` = 25
  - `AtrPeriod` = 200
  - `SuperTrendFactor` = 2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: DMI, Bollinger Bands, Momentum, MACD, SuperTrend, Aroon
  - Stops: Nein
  - Komplexität: Fortgeschritten
  - Zeitrahmen: Intraday (1m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Hoch
