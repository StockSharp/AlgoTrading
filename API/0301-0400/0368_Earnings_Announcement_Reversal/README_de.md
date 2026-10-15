# Gewinnankündigungs-Umkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Gewinnankündigungs-Umkehr-Strategie** shortet jüngste Gewinner und kauft jüngste Verlierer an Earnings-Ankündigungstagen.

## Details
- **Einstiegskriterien**: Am Earnings-Tag Short auf Aktien mit positiven jüngsten Renditen und Kauf jener mit negativen Renditen.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Position nach Signal angepasst; keine explizite Halteregel.
- **Stops**: Nein.
- **Standardwerte**:
  - `LookbackDays = 5`
  - `HoldingDays = 3`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Event-driven
  - Richtung: Beide
  - Indikatoren: Returns
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
