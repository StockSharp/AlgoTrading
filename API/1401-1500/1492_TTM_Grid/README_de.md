# TTM-Grid-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die TTM-Grid-Strategie baut Kauf- und Verkaufs-Grids basierend auf einem einfachen TTM-Zustand auf, der aus dem EMA von Hochs und Tiefs abgeleitet wird. Das Grid wird zurückgesetzt, wenn sich der Zustand ändert, und Orders werden platziert, wenn der Preis ein Grid-Level berührt.

## Details

- **Einstiegskriterien**: Preis erreicht Grid-Level gemäß TTM-Zustand.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Keine (Positionen akkumulieren sich).
- **Stops**: Nein.
- **Standardwerte**:
  - `TtmPeriod` = 6
  - `GridLevels` = 5
  - `GridSpacing` = 0.01m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Grid
  - Richtung: Beide
  - Indikatoren: EMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
