# TradingViewTo Strategie-Vorlage mit Dynamischen Alerts
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Vorlagenstrategie, die Positionen auf Basis von RSI-Niveaus eröffnet und Trades mit prozentualem Stop-Loss und Take-Profit verwaltet.

## Details
- **Einstiegskriterien**:
  - **Long**: RSI > `UpperLevel`
  - **Short**: RSI < `LowerLevel`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Stop-Loss oder Take-Profit
- **Stops**: Prozentualer Stop-Loss und Take-Profit
- **Standardwerte**:
  - `RsiLength` = 14
  - `UpperLevel` = 60
  - `LowerLevel` = 40
  - `StopLossPct` = 2m
  - `TakeProfitPct` = 4m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: RSI
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
