# Strategie HMA Crossover RSI Stochastic Trailing Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die den Crossover von schneller und langsamer HMA mit RSI- und geglättetem Stochastic-Filter verwendet. Eröffnet Long, wenn die schnelle HMA die langsame von unten kreuzt und RSI sowie Stochastic unter den Schwellenwerten liegen; bei umgekehrter Bedingung Short. Ein Trailing-Stop verwaltet die Ausstiege.

## Details

- **Einstiegskriterien**: Schnelle HMA kreuzt langsame von unten mit RSI und Stochastic unter den Schwellenwerten.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Trailing-Stop oder entgegengesetztes Signal.
- **Stops**: Trailing-Prozent.
- **Standardwerte**:
  - `FastHmaLength` = 5
  - `SlowHmaLength` = 20
  - `RsiPeriod` = 14
  - `RsiBuyLevel` = 45
  - `RsiSellLevel` = 60
  - `StochLength` = 14
  - `StochSmooth` = 3
  - `StochBuyLevel` = 39
  - `StochSellLevel` = 63
  - `TrailingPercent` = 5
  - `CandleType` = TimeSpan.FromHours(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: HMA, RSI, Stochastic
  - Stops: Trailing
  - Komplexität: Grundlegend
  - Zeitrahmen: 1h
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
