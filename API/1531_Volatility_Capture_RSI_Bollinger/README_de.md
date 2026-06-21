# Volatilitätserfassung RSI-Bollinger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie kombiniert dynamische Bollinger-Bänder mit einem optionalen RSI-Filter, um Volatilitätsschwankungen zu erfassen.

## Details
- **Einstiegskriterien**: Preiskreuzung der adaptiven Bollinger-Band mit optionaler RSI-Bestätigung.
- **Long/Short**: Konfigurierbar über `Direction`.
- **Ausstiegskriterien**: Preiskreuzung der gegenüberliegenden Seite des Trailing-Bands.
- **Stops**: Nein.
- **Standardwerte**:
  - `BollingerLength` = 50
  - `Multiplier` = 2.7183m
  - `UseRsi` = true
  - `RsiPeriod` = 10
  - `RsiSmaPeriod` = 5
  - `BoughtRangeLevel` = 55m
  - `SoldRangeLevel` = 50m
  - `Direction` = TradeDirection.Both
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Volatilität
  - Richtung: Konfigurierbar
  - Indikatoren: Bollinger, RSI
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
