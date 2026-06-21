# Supertrend-Strategie mit Festem TP Unified und Zeitfilter MSK
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf dem Supertrend-Indikator mit festem prozentualem Take-Profit, optionalem Preisfilter und Zeitfilter in der Moskauer Zeitzone.

## Details
- **Einstiegskriterien**: Supertrend-Richtungswechsel mit optionalen Preis- und Zeitfiltern
- **Long/Short**: Konfigurierbar (Long, Short oder beide)
- **Ausstiegskriterien**: Fester Take-Profit oder entgegengesetztes Signal
- **Stops**: Nur Take-Profit
- **Standardwerte**:
  - `AtrPeriod` = 23
  - `Factor` = 1.8m
  - `TakeProfitPercent` = 1.5m
  - `PriceFilter` = 10000m
  - `TimeFrom` = 0
  - `TimeTo` = 23
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Supertrend
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
