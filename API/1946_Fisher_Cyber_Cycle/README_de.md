# Fisher Cyber Cycle-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie wendet die Fisher-Transformation auf Ehlers' Cyber-Cycle-Indikator an. Eine Long-Position wird eröffnet, wenn die Fisher-Linie ihre Triggerlinie nach oben kreuzt, während eine Short-Position bei einem Abwärtskreuz eröffnet wird. Positionen werden beim entgegengesetzten Kreuz geschlossen oder umgekehrt.

## Details

- **Einstiegskriterien**:
  - **Long**: `Fisher > Trigger` && `vorheriger Fisher <= vorheriger Trigger`
  - **Short**: `Fisher < Trigger` && `vorheriger Fisher >= vorheriger Trigger`
- **Ausstiegskriterien**:
  - Entgegengesetztes Kreuz von Fisher und Trigger
- **Stops**: Keine
- **Standardwerte**:
  - `Alpha` = 0.07
  - `Length` = 8
  - `Candle Type` = 8-Stunden-Zeitrahmen
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long und Short
  - Indikatoren: Fisher Transform, Cyber Cycle
  - Stops: Keine
  - Komplexität: Moderat
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
