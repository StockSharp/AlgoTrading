# Manadi Kauf/Verkauf EMA MACD RSI Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

EMA-Kreuzungsstrategie mit MACD- und RSI-Bestätigungen. Markteinstiege mit festem prozentualen Stop-Loss und Take-Profit.

## Details

- **Einstiegskriterien**: EMA-Kreuzung mit MACD-Übereinstimmung und RSI-Grenzen.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Prozentualer Stop-Loss oder Take-Profit.
- **Stops**: Prozentbasiert.
- **Standardwerte**:
  - `FastEmaLength` = 9
  - `SlowEmaLength` = 21
  - `RsiLength` = 14
  - `RsiUpperLong` = 70
  - `RsiLowerLong` = 40
  - `RsiUpperShort` = 60
  - `RsiLowerShort` = 30
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `TakeProfitPercent` = 0.03
  - `StopLossPercent` = 0.015
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: EMA, MACD, RSI
  - Stops: Ja
  - Komplexität: Anfänger
  - Zeitrahmen: Intraday (1m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
