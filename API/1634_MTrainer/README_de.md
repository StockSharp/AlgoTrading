# MTrainer-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die MTrainer-Strategie repliziert das MT4-MTrainer-Skript. Sie eröffnet eine Position, wenn der Preis eine vordefinierte Einsteigslinie erreicht, und verwaltet sie mit Stop-Loss, Take-Profit und optionalen Teilschluss-Linien. Die Strategie ist für manuelles Üben im visuellen Tester konzipiert.

## Details

- **Einstiegskriterien**: Preis kreuzt die Einsteigslinie
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss, Take-Profit oder Teilschluss
- **Stops**: Ja
- **Standardwerte**:
  - `EntryPrice` = 0
  - `TakeProfitPrice` = 0
  - `StopLossPrice` = 0
  - `PartialClosePercent` = 0
  - `PartialClosePrice` = 0
  - `Volume` = 1
- **Filter**:
  - Kategorie: Hilfsprogramm
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
