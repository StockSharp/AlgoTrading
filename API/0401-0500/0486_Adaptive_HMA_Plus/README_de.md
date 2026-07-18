# Adaptive HMA-Plus-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Adaptive Hull Moving Average-Strategie, die ihre Periode basierend auf Volatilität oder Volumen anpasst. Sie eröffnet Long- oder Short-Positionen, wenn die HMA-Steigung bei aktiven Marktbedingungen in die Trendrichtung zeigt.

## Details

- **Einstiegskriterien**: Signale basierend auf adaptiver HMA, ATR oder Volumen.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `MinPeriod` = 172
  - `MaxPeriod` = 233
  - `AdaptPercent` = 0.031m
  - `FlatThreshold` = 0m
  - `UseVolume` = false
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: MA, ATR, Volume
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

