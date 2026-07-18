# Zeitbereich-Statistik-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Sammelt einfache Statistiken zwischen ausgewählten Balkenindizes.
Protokolliert Durchschnittspreis, normalisierten Bereich, prozentuale Änderung, durchschnittliches Volumen und Lückenanzahl.
Geht long, wenn die Periode positiv endet, und short, wenn sie negativ endet.

## Details

- **Einstiegskriterien**: prozentuale Änderung bei `EndIndex` bestimmt die Richtung
- **Long/Short**: Beide
- **Ausstiegskriterien**: keine
- **Stops**: Nein
- **Standardwerte**:
  - `StartIndex` = 9000
  - `EndIndex` = 10000
- **Filter**:
  - Kategorie: Statistik
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
