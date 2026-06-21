# Verbesserte Ichimoku-Cloud-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Nur-Long-Ichimoku-Strategie mit einem 171-Tage-EMA-Filter. Die Strategie kauft, wenn Span A über Span B liegt, der Preis das Hoch von vor 25 Bars bricht, Tenkan-sen über Kijun-sen liegt und der Schlusskurs über dem EMA liegt. Die Position wird geschlossen, wenn Tenkan unter Kijun fällt.

## Details

- **Einstiegskriterien**: spanA > spanB, close > high[25], Tenkan > Kijun, close > EMA.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Tenkan < Kijun.
- **Stops**: Nein.
- **Standardwerte**:
  - `ConversionPeriods` = 7
  - `BasePeriods` = 211
  - `LaggingSpan2Periods` = 120
  - `Displacement` = 41
  - `EmaPeriod` = 171
  - `StartDate` = 2018-01-01
  - `EndDate` = 2069-12-31
  - `CandleType` = TimeSpan.FromDays(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Nur Long
  - Indikatoren: Ichimoku, EMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
