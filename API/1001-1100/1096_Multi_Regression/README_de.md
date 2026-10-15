# Multi-Regressions-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Handelt, wenn der Preis eine Regressionslinie kreuzt, und verwaltet das Risiko mit volatilitätsbasierten Grenzen. Optionale Stop-Loss- und Take-Profit-Niveaus werden aus einem ausgewählten Risikomaß abgeleitet.

## Details

- **Einstiegskriterien**: Preis kreuzt über oder unter den Regressionswert.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Entgegengesetztes Signal oder wenn der Preis die ausgewählten Grenzen erreicht.
- **Stops**: Optional, basierend auf `UseStopLoss` und `UseTakeProfit`.
- **Standardwerte**:
  - `Length` = 90
  - `RiskMeasure` = Atr
  - `RiskMultiplier` = 1
  - `UseStopLoss` = true
  - `UseTakeProfit` = true
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: LinearRegression, ATR/StdDev/Bollinger/Keltner
  - Stops: Optional
  - Komplexität: Mittel
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
