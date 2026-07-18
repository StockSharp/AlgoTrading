# ICT Master Suite Trading IQ-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die ICT Master Suite-Strategie handelt Ausbrüche aus dem Tageshoch und -tief der Handelssitzung. Wenn der Preis über das Sitzungshoch schließt, eröffnet die Strategie eine Long-Position; wenn der Preis unter das Sitzungstief schließt, wird eine Short-Position eingegangen. Positionen werden mit einem ATR-basierten Trailing-Stop verwaltet.

## Details

- **Einstiegskriterien**:
  - Der Preis schließt über dem aktuellen Sitzungshoch (Long).
  - Der Preis schließt unter dem aktuellen Sitzungstief (Short).
- **Long/Short**: Long und Short.
- **Ausstiegskriterien**:
  - ATR-basierter Trailing-Stop.
- **Stops**: ATR-Trailing-Stop.
- **Standardwerte**:
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 1.5
  - `AllowLong` = true
  - `AllowShort` = true
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: ATR
  - Stops: Ja
  - Komplexität: Niedrig
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
