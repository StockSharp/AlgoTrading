# Supertrend Advance Pullback-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Supertrend Advance Pullback kombiniert Supertrend mit Pullback- oder Trendwechsel-Einstiegen. Optionale EMA-, RSI-, MACD- und CCI-Filter verfeinern die Signale.

## Details

- **Einstiegskriterien**: Supertrend-Pullback oder -Wechsel mit optionalen EMA-, RSI-, MACD-, CCI-Filtern
- **Long/Short**: Beide
- **Ausstiegskriterien**: entgegengesetztes Signal
- **Stops**: Nein
- **Standardwerte**:
  - `AtrLength` = 10
  - `Factor` = 3
  - `EmaLength` = 200
  - `UseEmaFilter` = true
  - `UseRsiFilter` = true
  - `RsiLength` = 14
  - `RsiBuyLevel` = 50
  - `RsiSellLevel` = 50
  - `UseMacdFilter` = true
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `UseCciFilter` = true
  - `CciLength` = 20
  - `CciBuyLevel` = 200
  - `CciSellLevel` = -200
  - `Mode` = Pullback
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Supertrend, EMA, RSI, MACD, CCI
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
