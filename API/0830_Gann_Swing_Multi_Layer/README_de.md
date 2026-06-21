# Gann Swing Multi-Schicht
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie auf Basis einer vereinfachten mehrschichtigen Gann-Swing-Analyse.
Handelt, wenn drei aufeinanderfolgende Swing-Richtungen übereinstimmen.

Der Ansatz folgt der klassischen Gann-Idee von Swing-Richtungswechseln.
Er wartet auf drei konsistente Swing-Verschiebungen, bevor eine Position eröffnet wird.

## Details

- **Einstiegskriterien**: Drei aufeinanderfolgende Swing-Richtungen in gleicher Ausrichtung.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetzte Swing-Richtung.
- **Stops**: Nein.
- **Standardwerte**:
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Swing
  - Richtung: Beide
  - Indikatoren: Gann
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (1m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
