# Price Based Z-Trend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Handelt auf Basis des Preis-Z-Scores relativ zu einem EMA. Einstieg erfolgt, wenn der Z-Score benutzerdefinierte Schwellenwerte kreuzt; unterstützt Long-, Short- oder beide Richtungen.

## Details

- **Einstiegskriterien**:
  - Z-Score kreuzt `Threshold` nach oben für Long.
  - Z-Score kreuzt `-Threshold` nach unten für Short.
- **Long/Short**: Konfigurierbar über `TradeDirection`.
- **Ausstiegskriterien**: Entgegengesetzter Schwellenwert-Crossover.
- **Stops**: Nein.
- **Standardwerte**:
  - `PriceDeviationLength` = 100
  - `PriceAverageLength` = 100
  - `Threshold` = 1
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Konfigurierbar
  - Indikatoren: EMA, StandardDeviation
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: 5 Minuten
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
