# Fisher Org Sign Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet den Fisher Transform-Indikator mit vordefinierten oberen und unteren Levels. Eine Long-Position wird eröffnet, wenn der Fisher-Wert den unteren Level nach oben kreuzt. Eine Short-Position wird eröffnet, wenn der Fisher-Wert den oberen Level nach unten kreuzt.

## Details

- **Einstiegskriterien**:
  - **Long**: `Fisher crosses above DownLevel`
  - **Short**: `Fisher crosses below UpLevel`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Gegensignal löst Positionsumkehr aus
- **Stops**: Nein
- **Standardwerte**:
  - `Fisher Length` = 7
  - `UpLevel` = 1.5
  - `DownLevel` = -1.5
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Fisher Transform
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Mittelfristig (H4)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
