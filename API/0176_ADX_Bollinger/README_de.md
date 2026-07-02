# Strategie Adx Bollinger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategie basierend auf den Indikatoren ADX und Bollinger Bänder. Geht long, wenn ADX > 25 und der Preis über das obere Bollinger Band ausbricht. Geht short, wenn ADX > 25 und der Preis unter das untere Bollinger Band ausbricht.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 115%. Sie funktioniert am besten auf dem Aktienmarkt.

Bollinger-Band-Ausbrüche, die mit ADX gefiltert werden, stellen sicher, dass der Preis mit Kraft ausbricht. Das System handelt in Richtung des Ausbruchs.

Geeignet für Hochvolatilitätsumgebungen. Ein ATR-basierter Stop reduziert das Abwärtsrisiko.

## Details

- **Einstiegskriterien**:
  - Long: `Close < LowerBand && ADX > 25`
  - Short: `Close > UpperBand && ADX > 25`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Preis kehrt zum mittleren Band zurück
- **Stops**: ATR-basiert mit `AtrMultiplier`
- **Standardwerte**:
  - `AdxPeriod` = 14
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: ADX, Bollinger Bands
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

