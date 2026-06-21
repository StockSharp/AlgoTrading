# Mateos Tageszeit-Analyse LE
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Öffnet eine Long-Position innerhalb eines festgelegten Intraday-Fensters und schließt sie später am Tag.

Diese Strategie eignet sich zur Erforschung von Tageszeit-Effekten.

## Details

- **Einstiegskriterien**: Die Zeit erreicht `StartTime` innerhalb des Datumsbereichs `From`-`Thru`.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Die Zeit erreicht `EndTime` (vor 20:00 Uhr).
- **Stops**: Nein.
- **Standardwerte**:
  - `CandleType` = TimeSpan.FromMinutes(1)
  - `StartTime` = 09:30
  - `EndTime` = 16:00
  - `From` = 2017-04-21
  - `Thru` = 2099-12-01
- **Filter**:
  - Kategorie: Zeitbasiert
  - Richtung: Nur Long
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
