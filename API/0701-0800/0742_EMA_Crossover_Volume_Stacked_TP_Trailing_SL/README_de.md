# EMA-Crossover mit Volumen + Gestaffeltem TP & Trailing-SL-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt EMA-Crossovers, gefiltert nach Volumen. Sie setzt zwei ATR-basierte Gewinnziele und zieht den verbleibenden Teil der Position mit einem Trailing-Stop nach, sobald sich der Kurs günstig entwickelt.

## Details

- **Einstiegskriterien**:
  - Schnelle EMA kreuzt über/unter die langsame EMA.
  - Volumen > Durchschnittsvolumen * `VolumeMultiplier`.
- **Long/Short**: Long und Short.
- **Ausstiegskriterien**:
  - Erster Take-Profit bei `TP1Multiplier * ATR` (33% der Position).
  - Zweiter Take-Profit bei `TP2Multiplier * ATR` (weitere 33%).
  - Trailing-Stop aktiviert sich, wenn der Kurs `TrailTriggerMultiplier * ATR` zurücklegt, und folgt mit `TrailOffsetMultiplier * ATR`.
- **Stops**: Nur Trailing-Stop.
- **Standardwerte**:
  - `FastLength` = 21
  - `SlowLength` = 55
  - `VolumeMultiplier` = 1.2
  - `AtrLength` = 14
  - `Tp1Multiplier` = 1.5
  - `Tp2Multiplier` = 2.5
  - `TrailOffsetMultiplier` = 1.5
  - `TrailTriggerMultiplier` = 1.5
  - `CandleType` = 5m
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long/Short
  - Indikatoren: EMA, ATR, Volume
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
