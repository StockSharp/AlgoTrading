# Strategie Omega Galsky
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

EMA-Crossover-Strategie mit Break-even-Stop-Logik.

## Details

- **Einstiegskriterien**: Schnelle EMA kreuzt langsame EMA mit Preisbestätigung durch EMA89.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Stop-Loss, Take-Profit oder gegenseitiges Signal.
- **Stops**: Ja.
- **Standardwerte**:
  - `Ema8Period` = 8
  - `Ema21Period` = 21
  - `Ema89Period` = 89
  - `FixedRiskReward` = 1.0m
  - `SlPercentage` = 0.001m
  - `TpPercentage` = 0.0025m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: EMA
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (1m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
