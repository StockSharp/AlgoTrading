# Fortgeschrittene Supertrend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die fortgeschrittene Supertrend-Strategie erweitert den klassischen Supertrend-Indikator um optionale RSI-, gleitender Durchschnitt- und Trendstärke-Filter. Sie geht long, wenn Supertrend auf bullisch wechselt, und short, wenn er bärisch wird. Optionaler Stop-Loss und Take-Profit werden aus ATR-Vielfachen abgeleitet.

## Details

- **Einstiegskriterien**:
  - Supertrend wechselt die Richtung (bärisch→bullisch für Long, bullisch→bärisch für Short).
  - Optionale Filter: RSI innerhalb festgelegter Grenzen, Preis relativ zu einem gleitenden Durchschnitt, Trendstärke und Ausbruchsbestätigung.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Gegenteiliges Supertrend-Signal oder optionale Stop-Loss/Take-Profit-Niveaus.
- **Stops**: ATR-basierter optionaler Stop-Loss und Take-Profit.
- **Standardwerte**:
  - `AtrLength` = 6
  - `Multiplier` = 3.0
  - `UseRsiFilter` = false
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `UseMaFilter` = true
  - `MaLength` = 50
  - `MaType` = Weighted
  - `UseStopLoss` = true
  - `SlMultiplier` = 3.0
  - `UseTakeProfit` = true
  - `TpMultiplier` = 9.0
  - `UseTrendStrength` = false
  - `MinTrendBars` = 2
  - `UseBreakoutConfirmation` = true
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long & Short
  - Indikatoren: Supertrend, RSI, Gleitender Durchschnitt
  - Stops: ATR-basiert
  - Komplexität: Moderat
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
