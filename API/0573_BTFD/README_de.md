# BTFD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Auf Volumen und RSI basierende Dip-Buying-Strategie mit fünf Take-Profit-Niveaus und einem Schutz-Stop.

## Details

- **Einstiegskriterien**: Volumenspike über SMA und RSI unter überverkauftem Niveau.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Fünf gestaffelte Take-Profit-Ziele oder Stop-Loss.
- **Stops**: Ja.
- **Standardwerte**:
  - `VolumeLength` = 70
  - `VolumeMultiplier` = 2.5
  - `RsiLength` = 20
  - `RsiOversold` = 30
  - `Tp1` = 0.4
  - `Tp2` = 0.6
  - `Tp3` = 0.8
  - `Tp4` = 1.0
  - `Tp5` = 1.2
  - `Q1` = 20
  - `Q2` = 40
  - `Q3` = 60
  - `Q4` = 80
  - `Q5` = 100
  - `StopLossPercent` = 5
  - `CandleType` = TimeSpan.FromMinutes(3)
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Long
  - Indikatoren: RSI, SMA
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (3m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
