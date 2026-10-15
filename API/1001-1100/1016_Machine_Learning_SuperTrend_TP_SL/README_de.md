# Machine Learning Supertrend TP SL Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf dem Supertrend-Indikator mit Trailing-Take-Profit und Stop-Loss.

Die Stop- und Gewinnlevels folgen der Supertrend-Linie, um anhaltende Bewegungen zu erfassen und Gewinne zu sichern, wenn der Schwung nachlässt.

## Details

- **Einstiegskriterien**: Preis kreuzt die Supertrend-Linie.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegensätzliches Signal oder Erreichen von Trailing Take-Profit/Stop-Loss.
- **Stops**: Ja, Trailing nach Supertrend.
- **Standardwerte**:
  - `AtrPeriod` = 4
  - `AtrFactor` = 2.94m
  - `StopLossMultiplier` = 0.0025m
  - `TakeProfitMultiplier` = 0.022m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: ATR, Supertrend
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
