# Zig Zag Aroon-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie kombiniert einfache ZigZag-Pivot-Erkennung mit dem Aroon-Indikator. Kauft, wenn Aroon Up Aroon Down von unten kreuzt und der letzte Pivot ein Hoch ist. Short-Positionen werden eröffnet, wenn Aroon Down Aroon Up von unten kreuzt und der letzte Pivot ein Tief ist.

## Details

- **Einstiegskriterien**: Aroon-Kreuzung mit passendem ZigZag-Pivot.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegenläufiges Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `ZigZagDepth` = 5
  - `AroonLength` = 14
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Aroon, ZigZag
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
