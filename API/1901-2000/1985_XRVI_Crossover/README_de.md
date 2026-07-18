# XRVI Crossover-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die XRVI Crossover-Strategie basiert auf dem Extended Relative Vigor Index (XRVI).
Der XRVI wird berechnet, indem der Relative Vigor Index geglättet und anschließend ein zweiter gleitender Durchschnitt zur Erzeugung einer Signallinie angewendet wird.
Die Strategie geht Long, wenn der XRVI die Signallinie von unten kreuzt, und Short, wenn er sie von oben kreuzt.
Bestehende Positionen werden bei entgegengesetzten Signalen umgekehrt.

## Details

- **Einstiegskriterien**: XRVI-Kreuzung seiner Signallinie
- **Long/Short**: Beide
- **Ausstiegskriterien**: Entgegengesetzte Kreuzung
- **Stops**: Nein
- **Standardwerte**:
  - `RviLength` = 10
  - `SignalLength` = 5
  - `CandleType` = H4-Zeitrahmen
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Beide
  - Indikatoren: Relative Vigor Index, Simple Moving Average
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
