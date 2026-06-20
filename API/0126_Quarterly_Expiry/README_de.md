# Strategie zum Quartalsverfall
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
In Quartalsverfall-Wochen werden Futures- und Optionskontrakte gerollt, was häufig Volatilität erzeugt, da Positionen geschlossen oder gerollt werden.
Kursschwankungen können sich beschleunigen, wenn Absicherungen angepasst werden und die Liquidität vorübergehend abnimmt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 115%. Sie funktioniert am besten am Aktienmarkt.

Die Strategie handelt in Richtung des vorherrschenden Trends zu Wochenbeginn und steigt vor dem Abrechnungstag aus, um das Chaos zu vermeiden.

Ein fester Stop hält das Risiko im Rahmen, falls die Volatilität zu extrem ausfällt.

## Details

- **Einstiegskriterien**: Kalendereffekt-Auslöser
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder entgegengesetztes Signal
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filter**:
  - Kategorie: Saisonalität
  - Richtung: Beide
  - Indikatoren: Saisonalität
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Ja
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

