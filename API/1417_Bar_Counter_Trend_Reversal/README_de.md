# Balken-Gegentrend-Umkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Sucht nach mehreren aufeinanderfolgenden steigenden oder fallenden Balken und geht Gegentrend-Trades ein, wenn der Preis Kanal-Extreme erreicht.

## Details

- **Einstiegskriterien**: Folge von Anstiegen oder Rückgängen mit optionaler Volumen- und Kanalbestätigung
- **Long/Short**: Beide
- **Ausstiegskriterien**: entgegengesetztes Signal
- **Stops**: Nein
- **Standardwerte**:
  - `NoOfRises` = 3
  - `NoOfFalls` = 3
  - `ChannelLength` = 20
  - `ChannelMultiplier` = 2
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Beide
  - Indikatoren: Keltner Channel oder Bollinger Bands
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
