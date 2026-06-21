# RCI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Verwendet den Rank Correlation Index und seinen gleitenden Durchschnitt zum Handeln von Kreuzungen. Eine Long-Position wird eröffnet, wenn der RCI über seinen gleitenden Durchschnitt steigt. Eine Short-Position wird eröffnet, wenn er darunter fällt. Die Handelsrichtung kann auf nur Long oder nur Short beschränkt werden.

## Details
- **Einstiegskriterien**: RCI kreuzt seinen gleitenden Durchschnitt.
- **Long/Short**: Konfigurierbar (beide, nur Long, nur Short).
- **Ausstiegskriterien**: Gegenläufige Kreuzung.
- **Stops**: Nein.
- **Standardwerte**:
  - `RciLength` = 10
  - `MaType` = SMA
  - `MaLength` = 14
  - `Direction` = Long & Short
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Konfigurierbar
  - Indikatoren: RCI, MA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
