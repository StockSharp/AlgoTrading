# Phasenkreuzungs-Strategie mit Zone
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Beispielstrategie geht long, wenn ein geglätteter SMA mit positivem Offset einen EMA mit negativem Offset von unten kreuzt. Die Position wird beim umgekehrten Crossover geschlossen.

## Details

- **Einstiegskriterien**: SMA + Offset kreuzt EMA - Offset von unten.
- **Long/Short**: nur Long.
- **Ausstiegskriterien**: umgekehrter Crossover.
- **Stops**: keine.
- **Standardwerte**:
  - `Length` = 20.
  - `Offset` = 0.5.
- **Filter**: keine.
- **Komplexität**: niedrig.
- **Zeitrahmen**: konfigurierbar.
