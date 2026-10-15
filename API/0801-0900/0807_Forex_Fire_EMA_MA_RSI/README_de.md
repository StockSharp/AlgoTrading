# Forex Fire EMA MA RSI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Multi-Zeitrahmen-Trendstrategie mit EMA-, MA- und RSI-Bestätigung. Verwendet 4h-Kerzen zur Konvergenz und 15m-Kerzen für Einstiege.

## Details

- **Einstiegskriterien**:
  - Long: Kurze EMA über langer EMA, Kurs über MA, schneller RSI über langsamem RSI und >50, steigendes Volumen mit Bestätigung vom höheren Zeitrahmen.
  - Short: Gegenteilige Bedingungen.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - EMA-Kreuzung oder RSI erreicht Schwellenwerte.
  - Optionaler Stop-Loss, Take-Profit, Trailing-Stop und ATR-basierter Ausstieg.
- **Stops**: Ja, konfigurierbar.
- **Standardwerte**:
  - `EmaShortLength` = 13
  - `EmaLongLength` = 62
  - `MaLength` = 200
  - `MaType` = MovingAverageTypeEnum.Simple
  - `RsiSlowLength` = 28
  - `RsiFastLength` = 7
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `UseStopLoss` = true
  - `StopLossPercent` = 2
  - `UseTakeProfit` = true
  - `TakeProfitPercent` = 4
  - `UseTrailingStop` = true
  - `TrailingPercent` = 1.5
  - `UseAtrExits` = true
  - `AtrMultiplier` = 2
  - `AtrLength` = 14
  - `EntryCandleType` = TimeSpan.FromMinutes(15).TimeFrame()
  - `ConfluenceCandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: EMA, MA, RSI, ATR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Multi-Zeitrahmen
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
