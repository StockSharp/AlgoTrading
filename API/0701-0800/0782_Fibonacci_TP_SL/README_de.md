# Fibonacci TP SL-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet Fibonacci-Retracement-Levels für Einstiege mit ATR-basiertem Stop-Loss und prozentualem Take-Profit. Der Handel wird durch einen Mindestabstand zwischen Trades in Bars und eine wöchentliche Gewinnbeschränkung limitiert.

## Details

- **Einstiegskriterien**:
  - **Long**: `Close <= Fib 38.2%` && `Close >= Fib 78.6%` && `Min bars since last trade`
  - **Short**: `Close <= Fib 23.6%` && `Close >= Fib 61.8%` && `Min bars since last trade`
- **Long/Short**: Beide Seiten
- **Ausstiegskriterien**:
  - `ATR stop-loss` oder `Take-profit`
- **Stops**: Ja
- **Standardwerte**:
  - `Take Profit %` = 4
  - `Min Bars Between Trades` = 10
  - `Lookback` = 100
  - `ATR Period` = 14
  - `ATR Multiplier` = 1.5
  - `Max Weekly Return` = 0.15

- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Highest, Lowest, ATR
  - Stops: Ja
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
