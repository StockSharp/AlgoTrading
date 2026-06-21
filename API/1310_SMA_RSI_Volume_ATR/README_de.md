# SMA RSI Volumen ATR-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert einen Einfachen Gleitenden Durchschnitt (SMA), den Relative Strength Index (RSI), Volumenbestätigung und einen ATR-basierten Volatilitätsfilter.
Sie kauft, wenn der Preis über dem SMA liegt, der RSI überverkauft ist, das Volumen seinen gleitenden Durchschnitt um einen Multiplikator übersteigt und die Volatilität steigt. Sie verkauft unter den entgegengesetzten Bedingungen.

Stops werden mit festen prozentualen Take-Profit- und Stop-Loss-Niveaus verwaltet.

## Details

- **Einstiegskriterien**:
  - **Long**: `Close > SMA` && `RSI < RsiOversold` && `Volume > AvgVolume * VolumeThreshold` && `ATR > ATR_{prev}`
  - **Short**: `Close < SMA` && `RSI > RsiOverbought` && `Volume > AvgVolume * VolumeThreshold` && `ATR > ATR_{prev}`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder Take-Profit
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `SmaLength` = 50
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `VolumeThreshold` = 1.5
  - `AtrLength` = 14
  - `TakeProfitPerc` = 1.5
  - `StopLossPerc` = 0.5
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: SMA, RSI, Volumen, ATR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
