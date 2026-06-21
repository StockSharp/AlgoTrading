# Mad Trader Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Mad Trader ist eine Trendfolge-Strategie, die aus dem ursprünglichen MQL-Experten "madtrader-8.7" konvertiert wurde. Sie kombiniert ATR- und RSI-Indikatoren, um Rücksetzer mit geringer Volatilität während eines entstehenden Trends zu identifizieren. Das System wartet darauf, dass der ATR unter einem bestimmten Schwellenwert liegt, aber noch steigt, und dass der RSI innerhalb eines allgemeinen bullischen oder bärischen Trends zunimmt. Wenn diese Bedingungen übereinstimmen und der Kerzenkörper innerhalb der definierten Grenzen liegt, öffnet die Strategie eine Marktorder in die vom RSI vorgeschlagene Richtung. Positionen werden durch einen Trailing-Stop und einen Basket-Profit-Mechanismus geschützt, der alle Trades schließt, sobald das Eigenkapital des Kontos das Zielwachstum erreicht.

## Details

- **Einstiegskriterien**:
  - ATR liegt unter `MaxAtr` und ist größer als der vorherige ATR-Wert.
  - Kerzenkörpergröße liegt zwischen `MinCandle` und `MaxCandle`.
  - Handelszeit liegt innerhalb von `[StartHour, EndHour)`.
  - RSI-Trend über 50 und aktueller RSI steigend, aber unter `RsiLowerLevel` → Kauf.
  - RSI-Trend unter 50 und aktueller RSI fallend, aber über `RsiUpperLevel` → Verkauf.
  - Erzwingt eine Mindestverzögerung von `TradeInterval` zwischen Trades.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Trailing-Stop ausgelöst.
  - Basket-Profit-Ziel erreicht (`BasketProfit` oder `BasketProfit * BasketBoost`).
- **Stops**: Trailing-Stop in Preispunkten gemessen.
- **Standardwerte**:
  - `AtrPeriod` = 14
  - `RsiPeriod` = 14
  - `TrendBars` = 60
  - `MinCandle` = 5
  - `MaxCandle` = 10
  - `MaxAtr` = 10
  - `RsiUpperLevel` = 50
  - `RsiLowerLevel` = 50
  - `StartHour` = 0
  - `EndHour` = 23
  - `TradeInterval` = 30 Minuten
  - `TrailingStop` = 7
  - `BasketProfit` = 1.05
  - `BasketBoost` = 1.1
  - `RefreshHours` = 24
  - `ExponentialGrowth` = 0.01
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: ATR, RSI
  - Stops: Trailing
  - Komplexität: Moderat
  - Zeitrahmen: Kurzfristig (5-Minuten-Kerzen)
  - Risikolevel: Mittel
