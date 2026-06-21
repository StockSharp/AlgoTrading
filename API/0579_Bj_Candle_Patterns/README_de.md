# Bj Kerzenmuster-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie sucht nach Dragonfly Doji und Gravestone Doji Kerzenmuster. Ein Dragonfly Doji mit langem unterem Docht kann eine bullische Umkehr signalisieren, während ein Gravestone Doji mit langem oberem Docht eine bärische Umkehr anzeigen kann. Die Strategie kauft nach einem Dragonfly Doji und verkauft nach einem Gravestone Doji.

## Details

- **Einstiegskriterien**: Dragonfly Doji → long; Gravestone Doji → short.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Gegenteiliges Signal oder nach Ermessen.
- **Stops**: Nein.
- **Standardwerte**:
  - `CandleType` = 15 Minuten
  - `DojiThreshold` = 0.1
- **Filter**:
  - Kategorie: Muster
  - Richtung: Beide
  - Indikatoren: Candlestick
  - Stops: Nein
  - Komplexität: Anfänger
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
