# Zufälliger Trailing-Stop-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Zufälliger Trailing-Stop-Strategie eröffnet zufällige Trades mit einem durch einen einfachen gleitenden Durchschnitt bestimmten Bias und verwaltet diese mit einem Trailing Stop.

## Details

- **Einstiegskriterien**: zufällige Richtung mit SMA-Bias
- **Long/Short**: Beide
- **Ausstiegskriterien**: Trailing Stop
- **Stops**: Ja
- **Standardwerte**:
  - `MinStopLevel` = 0.00036
  - `TrailingStep` = 0.00001
  - `SleepMinutes` = 5
  - `SmaPeriod` = 100
  - `Volume` = 0.1
- **Filter**:
  - Kategorie: Experimentell
  - Richtung: Beide
  - Indikatoren: SMA
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: 1m
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Hoch
