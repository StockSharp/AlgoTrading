# MA mit logistischer Funktion
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Trotz des historischen Namens ist diese Implementierung eine Kreuzungsstrategie mit zwei EMAs und berechnet kein logistisches Modell. Kreuzungen auf abgeschlossenen Kerzen eröffnen oder drehen die Position; prozentuale Take-Profits und Stop-Losses werden ab den Ausführungspreisen gemessen.

## Details
- **Daten**: Preiskerzen.
- **Einstiegskriterien**:
  - **Long**: Die schnelle EMA kreuzt die langsame EMA nach oben.
  - **Short**: Die schnelle EMA kreuzt die langsame EMA nach unten.
- **Ausstiegskriterien**: `TakeProfitPercent` oder `StopLossPercent` wird erreicht; eine Gegenkreuzung dreht die Position.
- **Pause**: fünf abgeschlossene Kerzen nach jeder Kreuzungsorder.
- **Standardwerte**:
  - `FastLength` = 12
  - `SlowLength` = 25
  - `TakeProfitPercent` = 8
  - `StopLossPercent` = 5
  - `CandleType` = 20 Minuten
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long & Short
  - Indikatoren: MA
  - Komplexität: Niedrig
  - Risikolevel: Mittel
