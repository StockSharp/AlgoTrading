# Strategie für ausstehende Orders
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die während bestimmter Stunden vier ausstehende Orders rund um den aktuellen Bid und Ask platziert. Sie pflegt kontinuierlich Buy-Limit-, Sell-Limit-, Buy-Stop- und Sell-Stop-Orders in einem konfigurierbaren Abstand vom Marktpreis. Jede ausstehende Order verwendet feste Stop-Loss- und Take-Profit-Abstände.

## Details

- **Einstiegskriterien**: Ausstehende Orders bei `Distance` Ticks vom aktuellen Bid/Ask innerhalb der erlaubten Stunden platzieren.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Take-Profit oder Stop-Loss relativ zum Einstiegspreis.
- **Stops**: Ja.
- **Standardwerte**:
  - `StartHour` = 6
  - `EndHour` = 20
  - `TakeProfit` = 20
  - `StopLoss` = 100
  - `Distance` = 15
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Range
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (1m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
