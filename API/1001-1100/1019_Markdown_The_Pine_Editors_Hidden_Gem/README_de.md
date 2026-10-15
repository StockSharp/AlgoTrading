# Markdown Der verborgene Schatz des Pine-Editors Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet Bollinger-Bänder, die auf einem einfachen gleitenden Durchschnitt basieren. Eine Long-Position wird eröffnet, wenn der Kurs über das obere Band schließt, und eine Short-Position, wenn er unter das untere Band schließt.

## Details

- **Einstiegskriterien**:
  - **Long**: Schlusskurs kreuzt über das obere Band.
  - **Short**: Schlusskurs kreuzt unter das untere Band.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Gegensätzliches Signal.
- **Stops**: Keine.
- **Standardwerte**:
  - `Length` = 50
  - `Multiplier` = 2
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Bollinger Bands
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
