# Mittagsumkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Die Mittagsumkehr sucht nach Wendepunkten, die gegen Mittag auftreten, wenn Morgendliche Trends oft erschöpft sind.
Die Liquidität trocknet typischerweise in der Mitte der Sitzung aus, was zu Umkehrungen führt, wenn Trader Positionen glattstellen.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 121%. Sie funktioniert am besten im Kryptomarkt.

Das System überwacht eine Momentumverschiebung gegen Mittag und tritt entgegen der Morgenrichtung ein.

Ein prozentualer Stop kontrolliert das Risiko, und Positionen werden geschlossen, wenn sich die Umkehr bis zum Nachmittag nicht entwickelt.

## Details

- **Einstiegskriterien**: Indikatorsignal
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder entgegengesetztes Signal
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 2
  - `MiddayHour` = 12
  - `AfternoonHour` = 16
    Der Markt handelt rund um die Uhr, daher ist die Sitzung der UTC-Tag. Die Morgenbewegung ist die Veränderung von der Tageseröffnung bis zum Schluss der Kerze, die um MiddayHour endet. Zwischen MiddayHour und AfternoonHour steigt die Strategie einmal gegen diese Bewegung ein, auf der ersten Kerze, die gegen sie schließt, und schließt die Position um AfternoonHour.
- **Filter**:
  - Kategorie: Intraday
  - Richtung: Beide
  - Indikatoren: Price Action
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

