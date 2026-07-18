# Renko-Chart-aus-Ticks-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Generiert Renko-Bausteine direkt aus Ticks und handelt, wenn sich die Richtung des Bausteins ändert. Demonstriert den Aufbau nicht zeitbasierter Kerzen mit der High-Level-API von StockSharp.

## Details

- **Einstiegskriterien**:
  - Wenn ein neuer abgeschlossener Baustein die Richtung umkehrt, in Richtung des neuen Bausteins einsteigen.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Position bei entgegengesetzter Bausteinrichtung umkehren.
- **Stops**: Nein.
- **Standardwerte**:
  - `BrickSize` = 10
  - `Volume` = 1
- **Filter**:
  - Kategorie: Renko
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Anfänger
  - Zeitrahmen: Tick-basiert
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
