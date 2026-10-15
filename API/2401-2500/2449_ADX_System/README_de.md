# Strategie ADX System
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **ADX System** Strategie handelt mit dem Average Directional Index und seinen DI-Komponenten. Sie öffnet eine Position, wenn der ADX steigt und eine der Richtungslinien über den ADX kreuzt. Positionen beinhalten feste Take-Profit- und Stop-Loss-Niveaus mit einem Trailing Stop zum Gewinnschutz.

## Details

- **Einstiegskriterien**
  - ADX steigt (vorheriger ADX unter aktuellem).
  - Für **Long**-Trades: Vorheriger +DI unter vorherigem ADX und aktueller +DI über aktuellem ADX.
  - Für **Short**-Trades: Vorheriger -DI unter vorherigem ADX und aktueller -DI über aktuellem ADX.
- **Ausstiegskriterien**
  - Entgegengesetztes Signal bei ADX- und DI-Linien.
  - Preis erreicht das Trailing Stop-Niveau.
  - Preis trifft den festen Take-Profit oder Stop-Loss.
- **Long/Short**: Beide Richtungen.
- **Stops**: Fester Stop-Loss, Take-Profit und Trailing Stop in absoluten Preiseinheiten.
- **Standardwerte**:
  - `AdxPeriod` = 14
  - `TakeProfit` = 15
  - `StopLoss` = 100
  - `TrailingStop` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: ADX, +DI, -DI
  - Stops: Ja
  - Komplexität: Anfänger
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
