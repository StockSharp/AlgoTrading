# MADH Gleitender-Durchschnitt-Differenz-Hann-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementiert den von John Ehlers beschriebenen MADH-Indikator. Die Strategie geht long, wenn der Indikator über null liegt, und short, wenn er darunter liegt.

## Details
- **Einstiegskriterien**: MADH > 0 für Longs, MADH < 0 für Shorts.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Umkehr bei entgegengesetztem Signal.
- **Stops**: Keine.
- **Standardwerte**:
  - `ShortLength` = 8
  - `DominantCycle` = 27
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: MADH
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
