# Fibonacci Auto Trend Scouter Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet zwei gleitende Extremwerte basierend auf Fibonacci-Zahlen, um aufkommende Trends zu erkunden. Das kurze Fenster (8) verfolgt aktuelle Hochs und Tiefs, während das lange Fenster (21) den Kontext liefert. Eine Long-Position wird eröffnet, wenn das kurzfristige Hoch das langfristige Hoch übersteigt. Eine Short-Position öffnet sich, wenn das kurzfristige Tief unter das langfristige Tief fällt.

## Details

- **Einstiegskriterien**:
  - **Long**: kurzfristiges Hoch > langfristiges Hoch.
  - **Short**: kurzfristiges Tief < langfristiges Tief.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Die Position wird beim entgegengesetzten Signal umgekehrt.
- **Stops**: Nein.
- **Standardwerte**:
  - `Short period` = 8
  - `Long period` = 21
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Mehrere
  - Stops: Nein
  - Komplexität: Einfach
  - Zeitrahmen: Mittelfristig
