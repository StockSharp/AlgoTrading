# Binary Wave StdDev-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die Signale von MA, MACD, CCI, Momentum, RSI und ADX mit konfigurierbaren Gewichten summiert.
Handelt in Richtung des kumulativen Scores, wenn die durch die Standardabweichung gemessene Volatilität einen Schwellenwert überschreitet.
Optionaler Stop-Loss und Take-Profit in Punkten.

## Details

- **Einstiegskriterien**:
  - Long: Score > 0 und StdDev >= EntryVolatility
  - Short: Score < 0 und StdDev >= EntryVolatility
- **Ausstiegskriterien**:
  - Volatilität fällt unter ExitVolatility
- **Stops**: Optional über `UseStopLoss` und `UseTakeProfit`
- **Standardwerte**:
  - `WeightMa` = 1
  - `WeightMacd` = 1
  - `WeightCci` = 1
  - `WeightMomentum` = 1
  - `WeightRsi` = 1
  - `WeightAdx` = 1
  - `MaPeriod` = 13
  - `FastMacd` = 12
  - `SlowMacd` = 26
  - `SignalMacd` = 9
  - `CciPeriod` = 14
  - `MomentumPeriod` = 14
  - `RsiPeriod` = 14
  - `AdxPeriod` = 14
  - `StdDevPeriod` = 9
  - `EntryVolatility` = 1.5
  - `ExitVolatility` = 1
  - `StopLossPoints` = 1000
  - `TakeProfitPoints` = 2000
  - `UseStopLoss` = false
  - `UseTakeProfit` = false
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: MA, MACD, CCI, Momentum, RSI, ADX, StandardDeviation
  - Stops: Optional
  - Komplexität: Moderat
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
