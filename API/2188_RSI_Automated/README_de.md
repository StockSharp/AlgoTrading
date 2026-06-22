# RSI Automatisierte Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Momentum-Strategie, die den Relative Strength Index (RSI) verwendet, um in extremen überverkauften und überkauften Bedingungen zu handeln.
Das System öffnet eine Long-Position, wenn der RSI unter den Überverkauft-Level fällt, und eine Short-Position, wenn der RSI über den Überkauft-Level steigt.
Positionen werden geschlossen, wenn der RSI zu einem mittleren Schwellenwert zurückkehrt oder wenn Stop-Loss-, Take-Profit- oder Trailing-Stop-Level ausgelöst werden.

## Details

- **Einstiegskriterien**: RSI kreuzt `Oversold` nach unten für Longs oder `Overbought` nach oben für Shorts.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: RSI kreuzt `ExitLevel`, Stop-Loss, Take-Profit oder Trailing-Stop.
- **Stops**: Ja, fester Stop-Loss, Take-Profit und optionaler Trailing-Stop.
- **Standardwerte**:
  - `RsiPeriod` = 14
  - `Overbought` = 75
  - `Oversold` = 25
  - `ExitLevel` = 50
  - `StopLossPoints` = 50
  - `TakeProfitPoints` = 150
  - `TrailingStopPoints` = 25
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Beide
  - Indikatoren: RSI
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (1m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
