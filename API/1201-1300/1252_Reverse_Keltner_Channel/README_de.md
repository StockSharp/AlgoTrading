# Umgekehrter Keltner-Kanal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die eintritt, wenn der Preis den Keltner-Kanal von außen wieder betritt, und auf das gegenüberliegende Band abzielt, mit optionalem ADX-Filter.

Die Strategie geht long, wenn der Preis das untere Keltner-Band von unten kreuzt und beim oberen Band oder an einem Stop bei der halben Kanalbreite schließt. Short-Trades sind symmetrisch. Ein ADX-Filter kann Trades auf schwache oder starke Trendregimes beschränken.

## Details

- **Einstiegskriterien**: Preis kreuzt äußeres Keltner-Band in den Kanal, optionaler ADX-Filter.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegenüberliegendes Band oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 2m
  - `StopLossFactor` = 0.5m
  - `AdxLength` = 14
  - `AdxThreshold` = 25m
  - `UseAdxFilter` = true
  - `WeakTrendOnly` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Beide
  - Indikatoren: Keltner, ADX
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
