# Forex Hammer und Hängender Mann Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt klassische Candlestick-Umkehrmuster: den bullishen Hammer und den bearishen Hängenden Mann. Sie geht nach einem Hammer long und nach einem Hängenden Mann short und hält die Position für eine feste Anzahl von Kerzen.

Die Position wird geschlossen, sobald der Haltezeitraum abläuft oder Schutz-Stops ausgelöst werden.

## Details

- **Einstiegskriterien**: Hammer für Long, Hängender Mann für Short.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Haltezeitraum oder Stop-Loss/Take-Profit.
- **Stops**: Ja.
- **Standardwerte**:
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `BodyLengthMultiplier` = 5
  - `ShadowRatio` = 1
  - `HoldPeriods` = 26
- **Filter**:
  - Kategorie: Muster
  - Richtung: Beide
  - Indikatoren: Candlestick
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
