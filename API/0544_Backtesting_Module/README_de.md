# Backtesting-Modul
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert das Standardverhalten des TradingView „Backtesting Module". Sie handelt einen einfachen gleitenden Durchschnitt-Crossover: Eine Long-Position wird eröffnet, wenn der 50-Perioden-SMA den 200-Perioden-SMA nach oben kreuzt, und eine Short-Position wird eröffnet, wenn der umgekehrte Crossover auftritt. Der Handel ist nur zwischen den festgelegten Start- und Endzeiten erlaubt.

## Details

- **Einstiegskriterien**: 50-Perioden-SMA kreuzt den 200-Perioden-SMA.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Gegenläufiger Crossover oder Verlassen des Zeitintervalls.
- **Stops**: Keine.
- **Standardwerte**:
  - `FastLength` = 50
  - `SlowLength` = 200
  - `StartTime` = 1 Jan 1980
  - `EndTime` = 31 Dec 2050
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: SMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Variabel
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
