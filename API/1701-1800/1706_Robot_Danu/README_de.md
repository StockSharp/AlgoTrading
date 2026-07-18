# Robot Danu-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die schnelle und langsame ZigZag-Niveaus vergleicht, die aus Kerzenhochs und -tiefs abgeleitet werden.
Eine Short-Position wird eröffnet, wenn das schnelle ZigZag-Niveau über dem langsamen liegt.
Eine Long-Position wird eröffnet, wenn das schnelle ZigZag-Niveau unter dem langsamen liegt.

## Details
- **Einstiegskriterien**: Vergleich schneller und langsamer ZigZag-Pivots.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetzte ZigZag-Relation.
- **Stops**: Keine.
- **Standardwerte**:
  - `FastLength` = 28
  - `SlowLength` = 56
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Highest, Lowest
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
