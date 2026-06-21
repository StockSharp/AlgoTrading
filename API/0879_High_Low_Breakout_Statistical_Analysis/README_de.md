# Hoch-Tief-Ausbruch-Strategie mit statistischer Analyse
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Handelt Ausbrüche aus den Hoch- oder Tief-Niveaus des ausgewählten Zeitrahmens. Die Strategie kann je nach konfigurierter Option long oder short einsteigen und schließt die Position nach einer festen Anzahl von Bars.

## Details

- **Einstiegskriterien**: Schlusskurs kreuzt das ausgewählte Hoch- oder Tief-Niveau.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal oder nach HoldingPeriod Bars.
- **Stops**: Nein.
- **Standardwerte**:
  - `EntryOption` = LongAtHigh
  - `TimeframeOption` = Daily
  - `HoldingPeriod` = 5
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: High, Low
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
