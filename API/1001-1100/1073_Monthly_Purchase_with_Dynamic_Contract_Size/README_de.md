# Monatliche Kaufstrategie mit dynamischer Kontraktgröße
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kauft an einem gewählten Tag jedes Monats eine dynamische Anzahl von Kontrakten unter Verwendung eines festen Prozentsatzes des Kontokapitals. Der Drawdown wird zu Informationszwecken verfolgt.

## Details

- **Einstiegskriterien**: Zeit >= StartDate UND Monatstag = BuyDay
- **Long/Short**: Nur Long
- **Ausstiegskriterien**: keine
- **Stops**: keine
- **Standardwerte**:
  - `CandleType` = 1 Tag
  - `StartDate` = 2010-01-01
  - `PercentOfEquity` = 0.03
  - `BuyDay` = 1
- **Filter**:
  - Kategorie: Cost-Averaging
  - Richtung: Long
  - Indikatoren: Nein
  - Stops: Nein
  - Komplexität: Anfänger
  - Zeitrahmen: Langfristig
  - Saisonalität: Monatlich
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
