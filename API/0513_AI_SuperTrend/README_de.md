# AI SuperTrend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die AI SuperTrend-Strategie kombiniert den SuperTrend-Indikator mit gewichteten gleitenden Durchschnitten des Preises und der SuperTrend-Linie. Ein Long-Trade wird eröffnet, wenn der SuperTrend nach oben dreht und die Preis-WMA über die SuperTrend-WMA steigt. Ein Short-Trade wird unter den entgegengesetzten Bedingungen eröffnet. Positionen werden mit einem dynamischen ATR-Trailing-Stop geschützt.

## Details

- **Einstiegskriterien**:
  - **Long**: SuperTrend-Richtung dreht nach oben und Preis-WMA liegt über SuperTrend-WMA.
  - **Short**: SuperTrend-Richtung dreht nach unten und Preis-WMA liegt unter SuperTrend-WMA.
- **Ausstiegskriterien**:
  - Trendumkehr oder ATR-Trailing-Stop.
- **Stops**: Dynamischer ATR-Trailing-Stop.
- **Standardwerte**:
  - `AtrPeriod` = 10
  - `AtrFactor` = 3
  - `PriceWmaLength` = 20
  - `SuperWmaLength` = 100
  - `EnableLong` = true
  - `EnableShort` = true
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: SuperTrend, WMA, ATR
  - Stops: Ja
  - Komplexität: Moderat
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
