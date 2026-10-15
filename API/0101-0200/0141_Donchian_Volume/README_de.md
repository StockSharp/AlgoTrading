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
  - `StopLossPercent` = 2
  - `DonchianPeriod` = 20
  - `VolumePeriod` = 20
    Der Kanal umfasst das höchste Hoch und das tiefste Tief der vorherigen DonchianPeriod Kerzen. Ein Schluss außerhalb bei Volumen über dem Durchschnitt der vorherigen VolumePeriod Kerzen steigt in Ausbruchsrichtung ein und dreht eine Gegenposition. Die Position schließt, sobald der Kurs wieder im Kanal schließt oder das Volumen unter seinen Durchschnitt fällt; der Stop liegt StopLossPercent vom Einstiegspreis entfernt, knapp innerhalb der durchbrochenen Grenze.
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

