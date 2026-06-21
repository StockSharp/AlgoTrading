# Adaptive Cyber Cycle-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie nutzt den Adaptive Cyber Cycle-Oszillator von John Ehlers. Sie berechnet einen geglätteten Preiszyklus und verwendet den vorherigen Wert als Triggerlinie. Eine Long-Position wird eröffnet, wenn der Zyklus die Triggerlinie nach oben kreuzt, und eine Short-Position bei einem Kreuz nach unten.

## Details

- **Einstiegskriterien**:
  - **Long**: Zyklus > vorheriger Zyklus.
  - **Short**: Zyklus < vorheriger Zyklus.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Das entgegengesetzte Signal schließt die Position und kehrt sie um.
- **Stops**: Standardmäßig keine; Schutz kann separat aktiviert werden.
- **Standardwerte**:
  - `Alpha` = 0.07
  - `Candle Type` = 1-Minuten-Zeitrahmen
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Adaptive Cyber Cycle
  - Stops: Optional
  - Komplexität: Moderat
  - Zeitrahmen: Intraday
