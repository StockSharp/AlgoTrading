# Dreieck-Ausbruch-Strategie für BTC (MARK804)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Handelt Ausbrüche aus dem SMA-Dreieck bei Volumsspitzen und verwaltet Positionen mit ATR-basierten Stops.

## Details

- **Einstiegskriterien**: Preis, der über die obere SMA-Linie oder unter die untere SMA-Linie kreuzt, mit Volumen über seinem SMA
- **Long/Short**: Beide
- **Ausstiegskriterien**: ATR-basierter Stop-Loss oder Take-Profit
- **Stops**: Ja
- **Standardwerte**:
  - `TriangleLength` = 50
  - `VolumeSmaLength` = 20
  - `AtrLength` = 14
  - `VolumeMultiplier` = 1.5
  - `AtrMultiplierSl` = 1.0
  - `AtrMultiplierTp` = 1.5
  - `CandleType` = 1 Stunde
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: SMA, ATR, Volumen
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
