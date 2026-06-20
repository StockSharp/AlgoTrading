# RSI Reversion
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf RSI Mean Reversion

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 115%. Sie funktioniert am besten am Aktienmarkt.

RSI Reversion geht davon aus, dass der Preis nach dem Erreichen extremer RSI-Werte zurückkehrt. Wenn der RSI unter den unteren Schwellenwert fällt, wird gekauft; wenn er über den oberen Schwellenwert steigt, wird verkauft. Positionen werden geschlossen, wenn der RSI sich wieder in Richtung neutraler Niveaus bewegt.

Die Extremwerte können für verschiedene Märkte kalibriert werden. Die Verwendung zusätzlicher Filter wie die Trendrichtung hilft, starke Bewegungen nicht zu früh zu kontern.


## Details

- **Einstiegskriterien**: Signale basierend auf RSI.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegensätzliches Signal oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `RsiPeriod` = 14
  - `OversoldThreshold` = 30m
  - `OverboughtThreshold` = 70m
  - `ExitLevel` = 50m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: RSI
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

