# RSI Box (Pseudo-Grid-Bot)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eine gitterbasierte Strategie, die Preisniveaus aus RSI-Überkauft- und Überverkauft-Signalen ableitet. Wenn RSI ein Extrem kreuzt, werden dynamische Gitterlinien aus jüngsten Hochs und Tiefs neu berechnet. Trades erfolgen, wenn der Preis über oder unter das nächste Gitterniveau bricht. Optionale Shorts werden unterstützt.

## Details

- **Einstiegskriterien**: Preis kreuzt die nächste Gitterlinie nach einem RSI-Extrem.
- **Long/Short**: Long standardmäßig, Shorts optional.
- **Ausstiegskriterien**: Preis kreuzt die entgegengesetzte Gitterlinie.
- **Stops**: Nein.
- **Standardwerte**:
  - `RsiPeriod` = 14
  - `Overbought` = 70
  - `Oversold` = 30
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `UseShorts` = false
- **Filter**:
  - Kategorie: Gitter
  - Richtung: Beide
  - Indikatoren: RSI, Highest, Lowest
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
