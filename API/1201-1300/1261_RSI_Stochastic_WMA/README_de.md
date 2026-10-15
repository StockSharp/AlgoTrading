# RSI Stochastic WMA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die RSI, Stochastischen Oszillator und einen gewichteten gleitenden Durchschnitt (WMA) kombiniert.
Kauft, wenn RSI überverkauft ist, %K über %D kreuzt und der Preis über der WMA liegt.
Leerverkauft, wenn RSI überkauft ist, %K unter %D kreuzt und der Preis unter der WMA liegt.

## Details

- **Einstiegskriterien**:
  - Long: `RSI < 30 && %K crosses above %D && Close > WMA`
  - Short: `RSI > 70 && %K crosses below %D && Close < WMA`
- **Long/Short**: Beide
- **Stops**: Keine
- **Standardwerte**:
  - `RsiLength` = 14
  - `StochK` = 14
  - `StochD` = 3
  - `WmaLength` = 21
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: RSI, Stochastic, WMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
