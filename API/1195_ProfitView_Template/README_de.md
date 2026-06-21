# ProfitView-Strategie-Vorlage
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eine grundlegende gleitende Durchschnitt-Crossover-Strategie mit konfigurierbaren Glättungstypen, abgeleitet von der ProfitView-Vorlage.

## Details

- **Einstiegskriterien**:
  - **Long**: MA1 kreuzt MA2 nach oben.
  - **Short**: MA1 kreuzt MA2 nach unten.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**: Entgegengesetzter Crossover.
- **Stops**: Nein.
- **Standardwerte**:
  - `MA1 Type` = SMA, `MA1 Length` = 10
  - `MA2 Type` = SMA, `MA2 Length` = 100
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Gleitende Durchschnitte
  - Stops: Nein
  - Komplexität: Grundlegend
