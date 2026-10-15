# Multi-Zeitrahmen RSI Kauf/Verkauf-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet RSI-Werte aus drei verschiedenen Zeitrahmen. Eine Long-Position wird eröffnet, wenn alle aktivierten RSI-Werte unter der Kaufschwelle liegen. Eine Short-Position wird eröffnet, wenn alle aktivierten RSI-Werte über der Verkaufsschwelle liegen. Eine Abkühlperiode verhindert aufeinanderfolgende Signale.

## Details

- **Einstiegskriterien**: Alle aktivierten RSIs unter/über den Schwellenwerten.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Entgegengesetztes Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `Rsi1Length` = 14
  - `Rsi2Length` = 14
  - `Rsi3Length` = 14
  - `Rsi1CandleType` = TimeSpan.FromMinutes(5)
  - `Rsi2CandleType` = TimeSpan.FromMinutes(15)
  - `Rsi3CandleType` = TimeSpan.FromMinutes(30)
  - `BuyThreshold` = 30m
  - `SellThreshold` = 70m
  - `CooldownPeriod` = 5
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: RSI
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Multi-Zeitrahmen
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
