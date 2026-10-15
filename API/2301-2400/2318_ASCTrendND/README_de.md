# ASCTrendND-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist vom ASCTrendND MQL5 Expert Advisor inspiriert. Sie verwendet einen Simple Moving Average als Haupttrendsignal, einen RSI-Filter zur Bestärkung der Stärke und einen ATR-basierten Trailing Stop zum Ausstieg aus Trades. Der Ansatz versucht, die ASCTrend + NRTR + TrendStrength-Logik in vereinfachter Form auf der StockSharp High-Level-API zu replizieren.

## Details

- **Einstiegskriterien:**
  - **Long:** Schlusskurs liegt über dem SMA und RSI > 50.
  - **Short:** Schlusskurs liegt unter dem SMA und RSI < 50.
- **Ausstiegskriterien:**
  - Trailing Stop basierend auf ATR * Multiplikator oder gegenläufiges Signal.
- **Stops:** Nur ATR-basierter Trailing Stop.
- **Standardwerte:**
  - `SmaPeriod` = 50
  - `RsiPeriod` = 14
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0
  - `CandleType` = 5-Minuten-Kerzen
- **Filter:**
  - Kategorie: Trendfolge
  - Richtung: Long/Short
  - Indikatoren: SMA, RSI, ATR
  - Stops: Trailing
  - Komplexität: Niedrig
  - Zeitrahmen: 5m
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
