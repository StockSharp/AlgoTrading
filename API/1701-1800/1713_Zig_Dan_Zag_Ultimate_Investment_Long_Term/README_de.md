# Zig Dan Zag Ultimative Langfrist-Investition
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Langfristige Investitionsstrategie, die ZigZag-Pivots mit einem langsamen SMA-Trendfilter kombiniert. Eine Position wird eröffnet, wenn sich ein neues ZigZag-Tief über dem SMA bildet, während Ausstiege bei entgegengesetzten Pivots unter dem SMA erfolgen.

## Details
- **Einstiegskriterien**: Neues ZigZag-Tief über dem SMA.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: ZigZag-Hoch unter dem SMA.
- **Stops**: Nein.
- **Standardwerte**:
  - `ZigzagDepth` = 12
  - `SmaLength` = 200
  - `CandleType` = TimeSpan.FromHours(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Nur Long
  - Indikatoren: Highest, Lowest, SimpleMovingAverage
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Langfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
