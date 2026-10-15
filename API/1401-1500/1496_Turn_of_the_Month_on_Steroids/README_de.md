# Turn of the Month on Steroids-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eine saisonale Strategie, die gegen Ende jedes Monats nach zwei aufeinanderfolgenden negativen Schlusskursen kauft und aussteigt, wenn ein kurzfristiger RSI überkaufte Bedingungen signalisiert.

## Details

- **Einstiegskriterien**: Monatstag über Schwellenwert und zweitägiger Rückgang
- **Long/Short**: Long
- **Ausstiegskriterien**: RSI über Schwellenwert
- **Stops**: Keine
- **Standardwerte**:
  - `DayOfMonth` = 25
  - `RsiLength` = 2
  - `RsiThreshold` = 65
- **Filter**:
  - Kategorie: Saisonalität
  - Richtung: Nur Long
  - Indikatoren: RSI
  - Stops: Nein
  - Komplexität: Anfänger
  - Zeitrahmen: Täglich
  - Saisonalität: Ja
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
