# Bollinger-Bounce-Umkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die Umkehrungen erfasst, wenn der Preis von den Bollinger Bands abprallt, mit MACD- und Volumenbestätigung. Das System begrenzt Einstiege auf fünf Trades pro Tag und wendet feste prozentuale Stop Loss und Take Profit an.

## Details

- **Einstiegskriterien**:
  - Long: `Close[1] < LowerBand[1] && Close > LowerBand && MACD > Signal && Volume >= AvgVolume * VolumeFactor`
  - Short: `Close[1] > UpperBand[1] && Close < UpperBand && MACD < Signal && Volume >= AvgVolume * VolumeFactor`
- **Long/Short**: Beide
- **Stops**: Prozentualer Take Profit und Stop Loss
- **Standardwerte**:
  - `BollingerPeriod` = 20
  - `BbStdDev` = 2m
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `VolumePeriod` = 20
  - `VolumeFactor` = 1m
  - `StopLossPercent` = 2m
  - `TakeProfitPercent` = 4m
  - `MaxTradesPerDay` = 5
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Beide
  - Indikatoren: Bollinger Bands, MACD, Volumen
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
