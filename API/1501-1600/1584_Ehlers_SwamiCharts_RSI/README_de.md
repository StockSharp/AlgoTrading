# Strategie Ehlers SwamiCharts RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Mittelt RSI-Werte der Perioden 2–48, um eine Farbkarte zu erstellen. Long bei durchschnittlich grüner Farbe, Short bei rot.

## Details

- **Einstiegskriterien**: Durchschnittliche Farbe grün (`Color1Avg` == 255 und `Color2Avg` > `LongColor`) für Long; rot (`Color1Avg` > `ShortColor` und `Color2Avg` == 255) für Short.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Gegensätzliches Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `LongColor` = 50
  - `ShortColor` = 50
  - `CandleType` = 5 minutes
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Beide
  - Indikatoren: RSI
  - Stops: Nein
  - Komplexität: Fortgeschritten
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
