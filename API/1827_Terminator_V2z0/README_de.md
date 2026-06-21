# Terminator V2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet den Moving Average Convergence Divergence (MACD)-Oszillator, um in beide Richtungen zu handeln. Eine Long-Position wird eröffnet, wenn die MACD-Linie die Signallinie von unten kreuzt. Eine Short-Position wird eröffnet, wenn die MACD-Linie die Signallinie von oben kreuzt. Positionen werden durch feste Stop-Loss- und Take-Profit-Levels geschützt, während ein optionaler Trailing-Stop Gewinne bei starken Trends sichern kann.

## Details

- **Einstiegskriterien**:
  - **Long**: `MACD` kreuzt die Signallinie nach oben.
  - **Short**: `MACD` kreuzt die Signallinie nach unten.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Stop-Loss- oder Take-Profit-Level wird erreicht.
  - Trailing-Stop wird ausgelöst.
- **Stops**: Ja, enthält Stop-Loss, Take-Profit und optionalen Trailing-Stop.
- **Standardwerte**:
  - `FastPeriod` = 14
  - `SlowPeriod` = 26
  - `SignalPeriod` = 1
  - `TakeProfit` = 500 Punkte
  - `StopLoss` = 2500 Punkte
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: MACD
  - Stops: Ja
  - Komplexität: Moderat
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
