# Risikomanagement und Positionsgröße - MACD-Beispiel
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie **Risikomanagement und Positionsgröße - MACD-Beispiel** demonstriert dynamisches Positionsgrößen-Management basierend auf dem aktuellen Eigenkapital. Sie stützt sich auf MACD-Crossover eines höheren Zeitrahmens in Kombination mit einem gleitenden Durchschnitt als Trendfilter.

## Details
- **Einstiegskriterien**: MACD-Linie kreuzt über/unter die Signallinie mit Trendbestätigung.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetzter MACD-Crossover.
- **Stops**: Keine.
- **Standardwerte**:
  - `InitialBalance = 10000m`
  - `LeverageEquity = true`
  - `MarginFactor = -0.5m`
  - `Quantity = 3.5m`
  - `MacdMaType = MovingAverageTypeEnum.EMA`
  - `FastMaLength = 11`
  - `SlowMaLength = 26`
  - `SignalMaLength = 9`
  - `MacdTimeFrame = TimeSpan.FromMinutes(30)`
  - `TrendMaType = MovingAverageTypeEnum.EMA`
  - `TrendMaLength = 55`
  - `TrendTimeFrame = TimeSpan.FromDays(1)`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: MACD, Moving Average
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
