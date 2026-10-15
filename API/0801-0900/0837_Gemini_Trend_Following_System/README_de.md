# Gemini Trendfolge-System
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Trendfolge-Strategie, die Rücksetzer zum 50-Tage-SMA in einem starken Aufwärtstrend kauft, der durch den 200-Tage-SMA und den jährlichen Rate-of-Change-Filter bestätigt wird.

## Details

- **Einstiegskriterien**: Der Kurs erholt sich über den SMA 50 nach einem kürzlichen Rücksetzer in einem bestätigten Aufwärtstrend.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Todeskreuz des SMA 50 unter den SMA 200 oder katastrophischer Stop.
- **Stops**: Optionaler katastrophischer Stop.
- **Standardwerte**:
  - `Sma50Length` = 50
  - `Sma200Length` = 200
  - `RocPeriod` = 252
  - `RocMinPercent` = 15m
  - `UseCatastrophicStop` = true
  - `CandleType` = TimeSpan.FromDays(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Long
  - Indikatoren: SMA, RateOfChange, Lowest
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
