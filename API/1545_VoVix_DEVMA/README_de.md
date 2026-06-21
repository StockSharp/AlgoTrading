# VoVix DEVMA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie analysiert das Volatilitätsverhalten anhand von Deviation Moving Averages (DEVMA), die auf der Standardabweichung des ATR basieren. Sie handelt Übergänge zwischen Kontraktions- und Expansionsphasen und verwendet ATR-basierte Ausstiege.

## Details

- **Einstiegskriterien**:
  - **Long**: Schnelles DEVMA kreuzt über das langsame DEVMA.
  - **Short**: Schnelles DEVMA kreuzt unter das langsame DEVMA.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - ATR-basierter Stop-Loss und Take-Profit.
- **Stops**: Ja, ATR-Multiplikatoren.
- **Standardwerte**:
  - `DeviationLookback` = 59
  - `FastLength` = 20
  - `SlowLength` = 60
  - `ATR SL Mult` = 2
  - `ATR TP Mult` = 3
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Mehrere
  - Stops: Ja
  - Komplexität: Komplex
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
