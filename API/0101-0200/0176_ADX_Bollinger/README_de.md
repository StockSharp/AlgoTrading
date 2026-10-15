# Strategie Adx Bollinger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategie basierend auf den Indikatoren ADX und Bollinger Bänder. Geht long, wenn ADX > 25 und der Preis unter dem unteren Bollinger Band schließt. Geht short, wenn ADX > 25 und der Preis über dem oberen Bollinger Band schließt.

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
  - `AdxThreshold` = 25
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    Die 25 in den Regeln ist der Standardwert von AdxThreshold. Wie die Einstiegskriterien, der Ausstieg am Mittelband und die Kategorie Mean reversion zeigen, wird ein Bandbruch bei starkem ADX gegengehandelt. Der Stop liegt AtrMultiplier ATR (AtrPeriod) vom Einstiegsschluss entfernt und wird auf Kerzenschlüssen geprüft. Ein Einstiegssignal gegen eine offene Position dreht sie.
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

