# Tägliche Supertrend EMA-Crossover RSI-Filter-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die EMA-Kreuzungen nur handelt, wenn der Supertrend die Richtung bestätigt und der RSI günstig ist. Verwendet ATR-basierte Stop-Loss- und Take-Profit-Niveaus.

## Details

- **Einstiegskriterien**:
  - Long: `Fast EMA` kreuzt `Slow EMA` von unten, Supertrend im Aufwärtstrend, `RSI < RsiOverbought`
  - Short: `Fast EMA` kreuzt `Slow EMA` von oben, Supertrend im Abwärtstrend, `RSI > RsiOversold`
- **Long/Short**: Beide
- **Ausstiegskriterien**: ATR-basierter Stop-Loss oder Take-Profit
- **Stops**: Ja
- **Standardwerte**:
  - `FastEmaLength` = 3
  - `SlowEmaLength` = 6
  - `AtrLength` = 3
  - `StopLossMultiplier` = 2.5m
  - `TakeProfitMultiplier` = 4m
  - `RsiLength` = 10
  - `RsiOverbought` = 65m
  - `RsiOversold` = 30m
  - `SupertrendMultiplier` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: EMA, Supertrend, RSI, ATR
  - Stops: ATR-Vielfache
  - Komplexität: Mittel
  - Zeitrahmen: Langfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
