# Bollinger Bands mit Trailing-Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Geht long, wenn der Kurs über dem oberen Bollinger Band schließt.
Steigt aus, wenn der Kurs unter das untere Band fällt oder ein ATR-basierter Trailing-Stop ausgelöst wird.

## Details

- **Einstiegskriterien**: Schluss oberhalb des oberen Bandes.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Schluss unterhalb des unteren Bandes oder Trailing-Stop ausgelöst.
- **Stops**: Trailing-Stop.
- **Standardwerte**:
  - `BbLength` = 20
  - `BbDeviation` = 2m
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
  - `MaType` = MovingAverageTypeEnum.Simple
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Nur Long
  - Indikatoren: Bollinger Bands, ATR
  - Stops: Ja
  - Komplexität: Anfänger
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
