# Supertrend und MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die Supertrend, MACD und EMA-200-Filter kombiniert.

## Details

- **Einstiegskriterien**: Preis relativ zu Supertrend und EMA, MACD-Linie gegenüber Signallinie.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: MACD-Crossover oder Stop basierend auf jüngsten Extremwerten.
- **Stops**: Highest/Lowest Trailing-Stops.
- **Standardwerte**:
  - `AtrPeriod` = 10
  - `Factor` = 3
  - `EmaPeriod` = 200
  - `StopLookback` = 10
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: SuperTrend, EMA, MACD, Highest, Lowest
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (1m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
