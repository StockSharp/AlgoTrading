# RSI ProPlus Bärenmärkte-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kauft, wenn der RSI ein bestimmtes Niveau nach oben kreuzt, und steigt bei einem festen Prozentsatz vom Einstiegspreis aus. Sie ist für bärische Marktbedingungen ausgelegt, bei denen schnelle Gegenbewegungen erwartet werden.

## Details

- **Einstiegskriterien**: RSI kreuzt das Niveau nach oben
- **Long/Short**: Long
- **Ausstiegskriterien**: Take-Profit bei einem Prozentsatz vom Einstieg
- **Stops**: Nein
- **Standardwerte**:
  - `RSI Period` = 11
  - `RSI Level` = 8
  - `Take Profit %` = 0.11
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Long
  - Indikatoren: RSI
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
