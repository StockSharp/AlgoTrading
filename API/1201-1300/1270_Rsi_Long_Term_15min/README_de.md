# RSI Langfrist-Strategie 15min
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert RSI-Überverkauft-Signale mit langfristigen gleitenden Durchschnitten und Volumenbestätigung, um Long-Positionen einzugehen. Es wird gekauft, wenn der RSI unter 30 liegt, der SMA(250) über dem SMA(500) steht und das Volumen deutlich über dem Durchschnitt liegt.

## Details

- **Einstiegskriterien**: RSI unter 30, SMA(250) über SMA(500) und Volumen mehr als 2,5-mal so hoch wie sein 20-Perioden-SMA
- **Long/Short**: Nur Long
- **Ausstiegskriterien**: SMA(250) kreuzt SMA(500) nach unten oder Stop-Loss
- **Stops**: Ja, fester Prozentsatz
- **Standardwerte**:
  - `RsiLength` = 10
  - `VolumeSmaLength` = 20
  - `Sma1Length` = 250
  - `Sma2Length` = 500
  - `VolumeMultiplier` = 2.5
  - `StopLossPercent` = 5
- **Filter**:
  - Kategorie: Trend
  - Richtung: Long
  - Indikatoren: RSI, SMA
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
