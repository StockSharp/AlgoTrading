# Strategische Multi-Schritt-Supertrend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie verwendet zwei Supertrend-Berechnungen, um Einstiege und Ausstiege mit konfigurierbaren mehrstufigen Take-Profits zu erkennen.

## Details

- **Einstiegskriterien**: Signale basierend auf den Richtungen zweier Supertrend-Indikatoren.
- **Long/Short**: Konfigurierbar.
- **Ausstiegskriterien**: Entgegengesetzter Supertrend oder Take-Profit-Niveaus.
- **Stops**: Take-Profit-Schritte.
- **Standardwerte**:
  - `UseTakeProfit` = true
  - `TakeProfitPercent1` = 6.0
  - `TakeProfitPercent2` = 12.0
  - `TakeProfitPercent3` = 18.0
  - `TakeProfitPercent4` = 50.0
  - `TakeProfitAmount1` = 12
  - `TakeProfitAmount2` = 8
  - `TakeProfitAmount3` = 4
  - `TakeProfitAmount4` = 0
  - `NumberOfSteps` = 3
  - `AtrPeriod1` = 10
  - `Factor1` = 3.0
  - `AtrPeriod2` = 5
  - `Factor2` = 4.0
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Konfigurierbar
  - Indikatoren: ATR, Supertrend
  - Stops: Take-Profit
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
