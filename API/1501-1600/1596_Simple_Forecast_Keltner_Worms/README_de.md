# Einfache Prognose - Keltner Worms-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie erstellt einen dynamischen Keltner-Kanal und handelt, wenn der Preis außerhalb des Bandes liegt.

## Details

- **Einstiegskriterien**:
  - Schlusskurs über dem oberen Kanal öffnet eine Long-Position.
  - Schlusskurs unter dem unteren Kanal öffnet eine Short-Position.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Ein entgegengesetztes Signal schließt die Position.
- **Stops**: Keine.
- **Standardwerte**:
  - `Length` = 10
- **Filter**:
  - Kategorie: Kanal
  - Richtung: Beide
  - Indikatoren: EMA, ATR
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
