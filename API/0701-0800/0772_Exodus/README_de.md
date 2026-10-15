# Exodus-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine vereinfachte Portierung des TradingView **EXODUS**-Skripts. Sie verwendet einen volumengewichteten Momentum-Oszillator (VWMO) zusammen mit dem Average Directional Index, um starke gerichtete Bewegungen zu erkennen.

## Details

- **Einstiegskriterien**
  - Long: `VWMO > VwmoThreshold` und `ADX > AdxThreshold`.
  - Short: `VWMO < -VwmoThreshold` und `ADX > AdxThreshold`.
- **Ausstiegskriterien**
  - Momentum kreuzt die Null-Linie oder ein entgegengesetztes Signal erscheint.
- **Indikatoren**
  - Average True Range
  - Average Directional Index
  - Simple Moving Average
- **Parameter**
  - `VwmoMomentum`, `VwmoVolume`, `VwmoSmooth`, `VwmoThreshold`
  - `AtrLength`, `AtrMultiplier`, `TpMultiplier`
  - `AdxLength`, `AdxThreshold`
  - `Volume`
  - `CandleType`
