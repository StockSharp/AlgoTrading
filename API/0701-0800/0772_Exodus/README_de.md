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

**Präzise vereinfachte VWMO-Formel und Standards:** `VwmoMomentum=10`, `VwmoVolume=20`, `VwmoSmooth=3`, `VwmoThreshold=25`, `AdxLength=14`, `AdxThreshold=25`, `AtrLength=14`, `AtrMultiplier=2`, `TpMultiplier=3`, `CandleType=1 Minute`. Für jede abgeschlossene Kerze `i` gilt `d_i=Close_i−Close_(i−VwmoMomentum)`. Roher `VWMO=100 × Σ(d_i×Volume_i)/Σ(|d_i|×Volume_i)` über die letzten `VwmoVolume` Kerzen (0 bei Nenner 0), danach SMA über `VwmoSmooth` Werte. Dies ist ein normalisierter **vorzeichenbehafteter volumengewichteter Impuls**, kein EMA-Kreuz. Long nur bei `VWMO > +VwmoThreshold && ADX > AdxThreshold`, Short bei `VWMO < −VwmoThreshold && ADX > AdxThreshold`. Gegenläufiges Signal kehrt die ganze Position mit einer Marktorder um; ohne Gegensignal beendet eine Nulldurchquerung des VWMO die Position. Native Stop-Distanz `Wilder ATR(AtrLength) × AtrMultiplier`, Gewinnziel-Distanz `Stop × TpMultiplier`. Die Level1-Bid/Ask-Überwachung läuft auch zwischen Kerzen. Keine Mehrfachorder bei aktiver Order und keine Zukunftsdaten.
