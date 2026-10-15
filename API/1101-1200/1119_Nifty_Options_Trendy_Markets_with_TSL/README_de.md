# Nifty Options Trendmarkt-Strategie mit TSL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ausbruch-Strategie mit Bollinger Bands sowie ADX- und Supertrend-Filtern. Einstiege erfordern einen Volumenausschlag. Positionen werden bei MACD-Kreuzungen, nachlassendem ADX oder einem ATR-basierten Trailing-Stop geschlossen.

## Details

- **Einstiegskriterien**:
  - Long: Preis kreuzt die obere Bollinger Band nach oben && ADX > Schwellenwert && Volumenausschlag && Preis über Supertrend
  - Short: Preis kreuzt die untere Bollinger Band nach unten && ADX > Schwellenwert && Volumenausschlag && Preis unter Supertrend
- **Long/Short**: Beide
- **Ausstiegskriterien**: MACD-Kreuzung, ADX-Rückgang oder ATR-Trailing-Stop
- **Stops**: ATR-Trailing-Stop
- **Standardwerte**:
  - `BollingerPeriod` = 20
  - `BollingerMultiplier` = 2m
  - `AdxLength` = 14
  - `AdxEntryThreshold` = 25m
  - `AdxExitThreshold` = 20m
  - `SuperTrendLength` = 10
  - `SuperTrendMultiplier` = 3m
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `AtrLength` = 14
  - `AtrMultiplier` = 1.5m
  - `VolumeSpikeMultiplier` = 1.5m
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Bollinger Bands, ADX, Supertrend, MACD, ATR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
