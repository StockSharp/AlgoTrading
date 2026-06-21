# Max Profit Min Loss Options-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert schnelle und langsame gleitende Durchschnitte mit RSI, MACD und einem Volumenfilter. Sie geht long, wenn Trend- und Momentum-Bedingungen übereinstimmen, und verwendet Stop-Loss und Trailing Profit für Ausstiege.

## Details

- **Einstiegskriterien**:
  - **Long**: schneller MA > langsamer MA, MACD kreuzt über Signallinie, RSI > überverkauft mit steigendem Muster, Volumen über Durchschnitt.
  - **Short**: schneller MA < langsamer MA, MACD kreuzt unter Signallinie, RSI < überkauft mit fallendem Muster, Volumen über Durchschnitt.
- **Ausstieg**: entgegengesetztes Signal oder Stop-Loss/Trailing Profit.
- **Stops**: prozentualer Stop-Loss und Trailing Profit.
- **Standardwerte**:
  - Länge schneller MA = 9
  - Länge langsamer MA = 21
  - RSI-Länge = 14
  - Volumen-SMA-Länge = 20
  - Stop-Loss = 1%
  - Trailing Profit = 4%
- **Indikatoren**: MA, RSI, MACD, Volumen-SMA
- **Zeitrahmen**: standardmäßig 5-Minuten-Kerzen
