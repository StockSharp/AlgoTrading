# JMA-Neigungsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie überwacht die Neigung des Jurik Moving Average (JMA). Eine Position wird eröffnet, wenn die Neigung die Null kreuzt oder ihre Richtung ändert, abhängig vom gewählten Modus.

## Details

- **Einstiegskriterien**:
  - **Long**: Neigung kreuzt unter null oder dreht aufwärts (modusabhängig).
  - **Short**: Neigung kreuzt über null oder dreht abwärts.
- **Long/Short**: Long und Short.
- **Ausstiegskriterien**:
  - Entgegengesetztes Signal kehrt die Position um.
- **Stops**: Keine.
- **Standardwerte**:
  - `JMA Length` = 14
  - `JMA Phase` = 0
  - `Mode` = Breakdown
  - `Candle Type` = Zeitrahmen 4h
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long & Short
  - Indikatoren: JMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: 4h
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
