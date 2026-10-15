# Tri-Monthly BTC Swing-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Tri-Monthly BTC Swing handelt mit EMA200, MACD-Kreuzung und RSI-Filter.
Die Strategie erlaubt nur einen Handel alle 90 Tage.

## Details

- **Einstiegskriterien**: Schlusskurs über EMA200, MACD-Linie über Signal, RSI über Schwellenwert und mindestens 90 Tage seit dem letzten Handel
- **Long/Short**: Long
- **Ausstiegskriterien**: MACD-Linie unter Signal oder RSI unter Schwellenwert
- **Stops**: Nein
- **Standardwerte**:
  - `EmaLength` = 200
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `RsiLength` = 14
  - `RsiThreshold` = 50
  - `TradeInterval` = 90 Tage
  - `CandleType` = 1 Tag
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long
  - Indikatoren: EMA, MACD, RSI
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
