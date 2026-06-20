# BB HeikinAshi-Einstieg
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Bollinger-Bands-Strategie mit Heikin Ashi Kerzen.

Das System wartet auf zwei oder drei aufeinanderfolgende bearische Heikin Ashi Bars, die das untere Bollinger Band berühren. Eine bullische Kerze, die wieder über dem Band schließt, löst einen Long-Einstieg aus. Shorts funktionieren in die entgegengesetzte Richtung. Die Hälfte der Position wird beim ersten Ziel geschlossen, der Rest wird mit einem Trailing-Stop geschützt.

## Details

- **Einstiegskriterien**: Umkehr aufeinanderfolgender Heikin Ashi Kerzen um die Bollinger Bands.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Teilweise Gewinnmitnahme und Trailing-Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `BollingerLength` = 20
  - `BollingerWidth` = 2
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Beide
  - Indikatoren: Heikin Ashi, Bollinger Bands
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (15m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
