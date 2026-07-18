# BuySell-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie emuliert den **BuySell** MetaTrader-Experten. Sie kombiniert einen gleitenden Durchschnitt mit dem Average True Range (ATR), um Trendumkehrungen zu erkennen.
Wenn der gleitende Durchschnitt nach oben dreht, gilt der Markt als bullisch; wenn er nach unten dreht, als bärisch.
Ein Trade wird nur eröffnet, wenn der vorherige Balken im entgegengesetzten Zustand war, was eine Umkehr bestätigt. Optionale Stop-Loss- und Take-Profit-Niveaus werden in Preispunkten angegeben.

## Details

- **Einstiegslogik**
  - **Long**: Der gleitende Durchschnitt wechselt von fallend zu steigend und der vorherige Balken war bärisch.
  - **Short**: Der gleitende Durchschnitt wechselt von steigend zu fallend und der vorherige Balken war bullisch.
- **Ausstiegslogik**
  - **Long**: Der gleitende Durchschnitt dreht nach unten oder Stop-Loss / Take-Profit wird ausgelöst.
  - **Short**: Der gleitende Durchschnitt dreht nach oben oder Stop-Loss / Take-Profit wird ausgelöst.
- **Indikatoren**: Einfacher gleitender Durchschnitt (SMA) und ATR.
- **Stops**: Sowohl Stop-Loss als auch Take-Profit in Punkten.
- **Berechtigungen**: Separate Flags erlauben oder verbieten das Öffnen/Schließen von Long- und Short-Positionen.
- **Standard-Zeitrahmen**: 4-Stunden-Kerzen.

## Parameter

| Name | Standard | Beschreibung |
| ---- | -------- | ------------ |
| `MaPeriod` | 14 | Periode des gleitenden Durchschnitts. |
| `AtrPeriod` | 60 | ATR-Periode. |
| `StopLoss` | 1000 | Stop-Loss in Preispunkten. |
| `TakeProfit` | 2000 | Take-Profit in Preispunkten. |
| `AllowLongEntry` | true | Berechtigung zum Öffnen von Long-Positionen. |
| `AllowShortEntry` | true | Berechtigung zum Öffnen von Short-Positionen. |
| `AllowLongExit` | true | Berechtigung zum Schließen von Long-Positionen. |
| `AllowShortExit` | true | Berechtigung zum Schließen von Short-Positionen. |
| `CandleType` | H4 | Für Berechnungen verwendeter Zeitrahmen. |

## Verwendung

1. Fügen Sie die Strategie Ihrer StockSharp-Lösung hinzu.
2. Konfigurieren Sie die Parameter nach Bedarf.
3. Führen Sie die Strategie im Live- oder Backtesting-Modus aus. Trades werden über `BuyMarket`- und `SellMarket`-Orders ausgeführt.

Der Ansatz eignet sich für Märkte, bei denen Trendumkehrungen mit Volatilitätsänderungen einhergehen, die vom ATR erfasst werden.
