# Adaptive Oszillator-Schwellenwert-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der Adaptive Oszillator-Schwellenwert verwendet den RSI mit einem dynamischen Schwellenwert basierend auf Bufis Adaptivem Schwellenwert (BAT). Er kauft, wenn der RSI unter ein festes Niveau oder einen adaptiven Schwellenwert fällt.

## Details

- **Einstiegskriterien**: RSI fällt unter festen oder adaptiven Schwellenwert
- **Long/Short**: Long
- **Ausstiegskriterien**: Fester Balkenausstieg oder Dollar-Stop-Loss
- **Stops**: Dollar-Stop-Loss
- **Standardwerte**:
  - `UseAdaptiveThreshold` = true
  - `RsiLength` = 2
  - `BuyLevel` = 14
  - `AdaptiveLength` = 8
  - `AdaptiveCoefficient` = 6
  - `ExitBars` = 28
  - `DollarStopLoss` = 1600
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Long
  - Indikatoren: RSI, StandardDeviation, LinearRegression
  - Stops: Dollar
  - Komplexität: Grundlegend
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
