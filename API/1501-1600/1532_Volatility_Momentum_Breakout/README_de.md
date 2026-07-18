# Volatilitäts-Momentum-Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kombiniert ATR-basierte Ausbruchsniveaus mit EMA-Trendfilter und RSI-Momentum, um starke Bewegungen zu erfassen.

## Details

- **Einstiegskriterien**: Preis schließt über/unter ATR-Ausbruchsniveaus mit EMA- und RSI-Bestätigung
- **Long/Short**: Beide
- **Ausstiegskriterien**: ATR-basierter Stop-Loss und Take-Profit mit 1:2 Risiko-Ertrags-Verhältnis
- **Stops**: ATR
- **Standardwerte**:
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 1.5
  - `Lookback` = 20
  - `EmaPeriod` = 50
  - `RsiPeriod` = 14
  - `RsiLongThreshold` = 50
  - `RsiShortThreshold` = 50
  - `RiskReward` = 2
  - `AtrStopMultiplier` = 1
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: ATR, EMA, RSI, Highest, Lowest
  - Stops: ATR
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
