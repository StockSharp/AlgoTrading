# MACD RSI Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
MACD RSI kombiniert das Momentum des MACD mit den Überkauft-/Überverkauft-Werten des RSI.
Wenn beide Indikatoren übereinstimmen, steigt die Wahrscheinlichkeit einer nachhaltigen Bewegung.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 130%. Sie funktioniert am besten am Aktienmarkt.

Die Strategie geht long, wenn der MACD nach oben kreuzt und der RSI aus dem überverkauften Bereich steigt, oder verkauft short, wenn der MACD nach unten kreuzt und der RSI aus dem überkauften Bereich fällt.

Stops auf Basis eines Kursanteils helfen, Verluste zu begrenzen, falls die Indikatoren nach dem Einstieg divergieren.

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
  - Indikatoren: MACD, RSI
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

