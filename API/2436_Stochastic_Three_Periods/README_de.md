# Stochastic Drei Perioden
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie **Stochastic Drei Perioden** richtet schnelle Stochastik-Signale mit der Bestätigung von zwei höheren Zeitrahmen aus. Trades werden eröffnet, wenn der schnelle Oszillator kreuzt, während beide höheren Zeitrahmen übereinstimmen.

## Details

- **Einstiegskriterien**: Schnelles %K kreuzt %D mit entgegengesetztem Ablesen vor `ShiftEntrance` Kerzen; beide Stochastiken in höheren Zeitrahmen zeigen %K über %D; Schlusskurs muss sich in Signalrichtung bewegen.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Entgegengesetzte schnelle Stochastik-Kreuzung, gemessen an der vorherigen Kerze.
- **Stops**: Fester Stop-Loss und Take-Profit in Punkten über `StartProtection`.
- **Standardwerte**:
  - `CandleType1` = 5m
  - `CandleType2` = 15m
  - `CandleType3` = 30m
  - `KPeriod1` = 5
  - `KPeriod2` = 5
  - `KPeriod3` = 5
  - `KExitPeriod` = 5
  - `ShiftEntrance` = 3
  - `TakeProfitPoints` = 30
  - `StopLossPoints` = 10
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Beide
  - Indikatoren: Stochastic
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
