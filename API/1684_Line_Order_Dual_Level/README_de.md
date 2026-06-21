# Doppelpegelorder-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine Adaption des MetaTrader-Expertenberaters "MyLineOrder" für die StockSharp API. Sie ermöglicht es einem Trader, horizontale Preisniveaus festzulegen, die automatische Marktorders auslösen, wenn der Preis sie berührt. Optionale Stop-Loss-, Take-Profit- und Trailing-Stop-Abstände werden in Pips angegeben, und das Handelsvolumen ist konfigurierbar.

Wenn der Marktpreis das Niveau **BuyPrice** erreicht, eröffnet die Strategie eine Long-Position. Das Berühren des Niveaus **SellPrice** öffnet eine Short-Position. Nach dem Einstieg überwacht die Strategie die Position und verlässt sie, wenn eine der Schutzbedingungen erfüllt ist: Stop-Loss, Take-Profit oder Trailing-Stop.

## Details

- **Einstiegskriterien**:
  - **Long**: Preis berührt oder überschreitet `BuyPrice`.
  - **Short**: Preis berührt oder fällt unter `SellPrice`.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Stop-Loss, Take-Profit oder Trailing-Stop.
- **Stops**:
  - `StopLossPips`, `TakeProfitPips`, `TrailingStopPips`.
- **Filter**:
  - Keine.
- **Parameter**:
  - `BuyPrice` – Niveau für Long-Einstieg.
  - `SellPrice` – Niveau für Short-Einstieg.
  - `StopLossPips` – Stop-Loss-Abstand in Pips.
  - `TakeProfitPips` – Take-Profit-Abstand in Pips.
  - `TrailingStopPips` – Trailing-Stop-Abstand in Pips.
  - `TradeVolume` – Ordervolumen.
  - `CandleType` – Zeitrahmen der Kerzen für die Preisüberwachung.
