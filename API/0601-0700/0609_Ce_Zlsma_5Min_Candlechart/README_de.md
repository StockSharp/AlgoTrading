# CE ZLSMA 5MIN Candlechart-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Trendfolgesystem, das ZLSMA auf Heikin-Ashi-Kerzen mit einem Chandelier-Exit-Filter verwendet. Kauft, wenn der Trend auf bullisch dreht und die Kerze über dem ZLSMA schließt.

## Details

- **Einstiegskriterien**:
  - Long: Richtung dreht aufwärts, Heikin-Ashi-Schluss über ZLSMA und Eröffnung
- **Long/Short**: Long
- **Ausstiegskriterien**:
  - Long: Schluss unter ZLSMA
- **Stops**: Keine
- **Standardwerte**:
  - `ZlsmaLength` = 50
  - `AtrPeriod` = 1
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long
  - Indikatoren: ZLSMA, ATR, Heikin Ashi
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
