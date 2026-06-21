# Multi-Indikator-Trendfolge-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

EMA-Crossover-Strategie mit RSI- und Volumen-Bestätigung. Verwendet ATR-basierte Stop-Loss- und Take-Profit-Niveaus.

## Details

- **Einstiegskriterien**: Schneller EMA kreuzt über/unter langsamem EMA mit RSI-Filter und hohem Volumen
- **Long/Short**: Beide
- **Ausstiegskriterien**: ATR-basierter Stop-Loss und Take-Profit
- **Stops**: Ja, ATR-basiert
- **Standardwerte**:
  - `CandleType` = 5 minute
  - `FastMaLength` = 10
  - `SlowMaLength` = 30
  - `RsiLength` = 14
  - `VolumeMaLength` = 20
  - `VolumeMultiplier` = 1.5
  - `AtrPeriod` = 14
  - `StopLossAtrMultiplier` = 2
  - `TakeProfitAtrMultiplier` = 3
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: EMA, RSI, ATR, Volume
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
