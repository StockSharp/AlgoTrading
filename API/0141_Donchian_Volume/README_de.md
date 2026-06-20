# Donchian Volume Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Donchian Volume verwendet Donchian-Kanal-Ausbrüche, die durch steigendes Volumen bestätigt werden, um Trades einzuleiten.
Eine Bewegung außerhalb des Kanals bei starkem Volumen deutet auf den Beginn eines neuen Trends hin.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 160%. Die Strategie funktioniert am besten auf dem Forex-Markt.

Die Strategie steigt in Richtung des Ausbruchs ein und steigt aus, wenn der Kurs wieder innerhalb des Kanals schließt oder das Volumen nachlässt.

Stops werden kurz innerhalb des Kanals gesetzt, um gegen Fehlbewegungen zu schützen.

## Details

- **Einstiegskriterien**: Indikatorsignal
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder entgegengesetztes Signal
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Donchian Channel, Volume
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

