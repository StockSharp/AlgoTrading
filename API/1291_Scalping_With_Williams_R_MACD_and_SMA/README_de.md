# Scalping-Strategie mit Williams %R, MACD und SMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Scalping-Strategie, die Williams %R, das MACD-Histogramm und einen einfachen gleitenden Durchschnitt auf Einminutenkerzen einsetzt.

## Details

- **Einstiegskriterien**: Williams %R kreuzt Aktivierungsniveaus und das MACD-Histogramm wechselt das Vorzeichen in Trendrichtung.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Das Histogramm kehrt seine Richtung um.
- **Stops**: Nein.
- **Standardwerte**:
  - `WilliamsLength` = 140
  - `MacdFast` = 24
  - `MacdSlow` = 52
  - `MacdSignal` = 9
  - `SmaLength` = 7
