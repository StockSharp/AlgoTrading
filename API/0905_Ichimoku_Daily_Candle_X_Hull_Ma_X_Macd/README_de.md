# Ichimoku Daily Candle X Hull MA X MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert Ichimoku-Vorlauflinien, die Richtung der Tageskerze, den Hull-Moving-Average-Trend und einen HMA-basierten MACD. Long-Positionen werden eröffnet, wenn alle Komponenten bullisch ausgerichtet sind; Shorts entstehen, wenn alle Bedingungen bärisch werden.

## Details

- **Einstiegskriterien**:
  - **Long**: HMA steigt, aktueller Preis über dem vorherigen HMA, aktuelle Tageskerze höher als die vorherige, SenkouA > SenkouB, MACD-Linie > Signal.
  - **Short**: HMA fällt, Preis unter dem vorherigen HMA, aktuelle Tageskerze niedriger als die vorherige, SenkouA < SenkouB, MACD-Linie < Signal.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**: Entgegengesetztes Signal.
- **Stops**: Keine.
- **Standardwerte**:
  - `HmaPeriod` = 14
  - `ConversionPeriod` = 9
  - `BasePeriod` = 26
  - `SpanPeriod` = 52
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `PriceSource` = Open
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Ichimoku, Hull MA, MACD
  - Stops: Keine
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
