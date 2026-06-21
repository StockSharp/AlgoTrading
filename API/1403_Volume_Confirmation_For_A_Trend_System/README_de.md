# Volumenbestätigung für ein Trend-System (Strategie)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet den Trend Thrust Indicator (TTI), den Volume Price Confirmation Indicator (VPCI) und den ADX, um Long-Trends zu bestätigen.

## Details
- **Einstiegskriterien**:
  - **Long**: ADX > 30, TTI > Signal, VPCI > 0.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - VPCI < 0.
- **Stops**: Nein.
- **Standardwerte**:
  - `ADX Length` = 14
  - `ADX Smoothing` = 14
  - `TTI Fast Average` = 13
  - `TTI Slow Average` = 26
  - `TTI Signal Length` = 9
  - `VPCI Short Avg` = 5
  - `VPCI Long Avg` = 25
- **Filter**:
  - Kategorie: Trend
  - Richtung: Long
  - Indikatoren: ADX, TTI, VPCI
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
