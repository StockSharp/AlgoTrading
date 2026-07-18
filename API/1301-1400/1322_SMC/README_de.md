# SMC-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die SMC-Strategie definiert Premium-, Gleichgewichts- und Discount-Zonen aus aktuellen Swing-Hochs und -Tiefs. Sie handelt in Discount- oder Premium-Zonen mit einem SMA-Trendfilter und einfacher Order-Block-Bestätigung.

## Details

- **Einstiegskriterien**: Preis in der Discount-Zone über SMA mit Order-Block-Unterstützung; Preis in der Premium-Zone unter SMA mit Order-Block-Widerstand
- **Long/Short**: Beide
- **Ausstiegskriterien**: entgegengesetztes Signal
- **Stops**: Nein
- **Standardwerte**:
  - `SwingHighLength` = 8
  - `SwingLowLength` = 8
  - `SmaLength` = 50
  - `OrderBlockLength` = 20
- **Filter**:
  - Kategorie: Zone
  - Richtung: Beide
  - Indikatoren: Highest, Lowest, SMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
