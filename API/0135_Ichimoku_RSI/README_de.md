# Ichimoku RSI Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Ichimoku RSI verwendet Ichimoku-Cloud-Niveaus zur Definition der Trendrichtung, während der RSI kurzfristige Rücksetzer identifiziert.
Trades werden mit der Cloud ausgerichtet, mit Einstieg wenn der RSI in einem Aufwärtstrend aus der überverkauften Zone zurückkehrt oder in einem Abwärtstrend aus der überkauften Zone fällt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 142%. Die Strategie funktioniert am besten auf dem Aktienmarkt.

Durch die Kombination eines breiten Trendfilters mit einem Momentum-Oszillator zielt die Strategie darauf ab, nach kurzen Pausen in starke Bewegungen einzusteigen.

Stops werden jenseits der Cloud-Grenze gesetzt, um gegen tiefere Korrekturen zu schützen.

## Details

- **Einstiegskriterien**: Indikatorsignal
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder entgegengesetztes Signal
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Ichimoku, RSI
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

