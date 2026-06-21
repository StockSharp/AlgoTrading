# Gold-Scalping-Strategie mit präzisen Einstiegen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Scalping-Strategie für Gold mit EMA-Trendfilter, RSI-Bereich und Engulfing-Mustern.

## Details

- **Einstiegskriterien**: EMA-Trendfilter mit RSI zwischen 45 und 55 sowie bullischem/bärischem Engulfing in der Nähe von EMA50.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Take-Profit oder Stop-Loss.
- **Stops**: ATR-basierter Stop-Loss und festes Pip-Ziel.
- **Standardwerte**:
  - `EmaFastPeriod` = 50
  - `EmaSlowPeriod` = 200
  - `RsiPeriod` = 14
  - `AtrPeriod` = 14
  - `RsiLower` = 45
  - `RsiUpper` = 55
  - `PipTarget` = 2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Scalping
  - Richtung: Beide
  - Indikatoren: EMA, RSI, ATR
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (1m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
