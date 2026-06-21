# Optimierte Heikin-Ashi-Strategie mit Kauf-/Verkaufsoptionen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Heikin-Ashi-Kerzen glätten die Preisdaten und heben die Trendrichtung hervor. Diese Strategie handelt jeweils nur eine Richtung: entweder Longs auf grünen Kerzen oder Shorts auf roten Kerzen innerhalb eines benutzerdefinierten Datumsbereichs. Optionale Stop-Loss- und Take-Profit-Niveaus bieten Risikokontrolle.

## Details

- **Einstiegskriterien**: Heikin-Ashi-Kerzenfarbwechsel.
- **Long/Short**: Konfigurierbar.
- **Ausstiegskriterien**: Gegenseitiges Signal oder Stop-Niveaus.
- **Stops**: Optional, prozentbasiert.
- **Standardwerte**:
  - `CandleType` = 1 day
  - `StartDate` = 2023-01-01
  - `EndDate` = 2024-01-01
  - `TradeType` = BuyOnly
  - `UseStopLoss` = true
  - `StopLossPercent` = 2
  - `UseTakeProfit` = true
  - `TakeProfitPercent` = 4
- **Filter**:
  - Kategorie: Trend
  - Richtung: Konfigurierbar
  - Indikatoren: Heikin-Ashi
  - Stops: Optional
  - Komplexität: Grundlegend
  - Zeitrahmen: Täglich
  - Saisonalität: Datumsbereich
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

