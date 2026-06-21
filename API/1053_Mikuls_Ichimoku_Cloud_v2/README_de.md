# Mikul's Ichimoku Cloud v2 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ausbruch-Strategie mit Ichimoku Cloud und optionalem gleitendem Durchschnittsfilter. Positionen werden durch einen Trailing-Stop (ATR, Prozent oder Ichimoku-Regeln) und optionalen Take-Profit verwaltet.

## Details

- **Einstiegskriterien**: Tenkan-sen kreuzt über Kijun-sen bei Preis über der Cloud, oder ein starker Ausbruch über eine grüne Cloud.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Trailing-Stop oder Ichimoku-Umkehr, optionaler Take-Profit.
- **Stops**: Trailing.
- **Standardwerte**:
  - `TrailSource` = `LowsHighs`
  - `TrailMethod` = `Atr`
  - `TrailPercent` = 10
  - `SwingLookback` = 7
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 1
  - `AddIchiExit` = false
  - `UseTakeProfit` = false
  - `TakeProfitPercent` = 25
  - `UseMaFilter` = false
  - `MaType` = `Ema`
  - `MaLength` = 200
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouBPeriod` = 52
  - `Displacement` = 26
  - `CandleType` = TimeSpan.FromHours(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Long
  - Indikatoren: Ichimoku, ATR
  - Stops: Trailing
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (1h)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
