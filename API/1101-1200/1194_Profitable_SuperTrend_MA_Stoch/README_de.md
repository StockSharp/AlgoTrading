# Profitable SuperTrend + MA + Stoch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die SuperTrend, gleitenden Durchschnitt-Crossover und den Stochastic-Oszillator kombiniert.

Sie zielt darauf ab, von SuperTrend identifizierte Trends zu erfassen und Einstiege mit EMA-Crossover und Stochastic-Niveaus zu bestätigen. Enthält optionale Take-Profit- und Stop-Loss-Ziele.

## Details

- **Einstiegskriterien**: Trend durch SuperTrend, EMA-Crossover, Stochastic-Schwellenwerte.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetzter EMA-Crossover oder TP/SL.
- **Stops**: Ja.
- **Standardwerte**:
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 3m
  - `MaFastPeriod` = 9
  - `MaSlowPeriod` = 21
  - `StochKPeriod` = 14
  - `StochDPeriod` = 3
  - `TakeProfitPercent` = 2m
  - `StopLossPercent` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: SuperTrend, EMA, Stochastic
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
