# Strategie JK BullP AutoTrader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der JK BullP AutoTrader ist ein Port des ursprünglichen MetaTrader-Expert-Advisors, der sich auf den Bulls-Power-Oszillator stützt. Er interpretiert die Beziehung zwischen zwei aufeinanderfolgenden Bulls-Power-Werten, um zu erkennen, wann die bullische Stärke oberhalb der Nulllinie nachlässt oder wenn sie unter null fällt und sich umkehrt. Long- und Short-Trades sind mit festen Stops und einem schrittweisen Trailing Stop geschützt, der sich anzieht, wenn der Trade profitabler wird.

## Details

- **Einstiegskriterien**: Verkaufen, wenn Bulls Power vor zwei Bars über dem vorherigen Bar liegt und der vorherige Bar über null liegt. Kaufen, wenn der vorherige Bulls-Power-Bar unter null liegt.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Fester Take-Profit, fester Stop-Loss oder Trailing-Stop wird erreicht. Entgegengesetzte Signale kehren die Position um.
- **Stops**: Fester Take-Profit, fester Stop-Loss, Trailing Stop.
- **Standardwerte**:
  - `BullsPeriod` = 13
  - `TakeProfitPoints` = 350
  - `StopLossPoints` = 100
  - `TrailingStopPoints` = 100
  - `TrailingStepPoints` = 40
  - `CandleType` = TimeSpan.FromHours(1)
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Beide
  - Indikatoren: Bulls Power
  - Stops: Fest + Trailing
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday / Swing (1H)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
