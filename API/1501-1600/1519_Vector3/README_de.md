# Vector3-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Handelt auf Basis der Ausrichtung von drei gleitenden Durchschnitten.
Geht long, wenn fast > middle > slow, und short, wenn fast < middle < slow.

## Details

- **Einstiegskriterien**: fast MA über middle und middle über slow (Long); umgekehrt für Short
- **Long/Short**: Beide
- **Ausstiegskriterien**: entgegengesetztes Signal
- **Stops**: Nein
- **Standardwerte**:
  - `FastLength` = 10
  - `MiddleLength` = 50
  - `SlowLength` = 100
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: SMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
