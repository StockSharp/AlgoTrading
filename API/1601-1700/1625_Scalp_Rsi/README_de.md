# Scalp RSI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Scalping-Strategie basierend auf schnellen RSI-Änderungen. Konvertiert aus dem MetaTrader-Skript `scalpen_rsi.mq4`.
Die Strategie eröffnet Trades, wenn der RSI scharf fällt oder steigt, und wendet feste Take-Profit- und Stop-Loss-Niveaus an.

## Details

- **Einstiegskriterien**:
  - **Kauf**: RSI-Wert vor `buy_period` Balken minus aktueller RSI ≥ `BuyMovement`,
    vorheriger RSI minus aktueller RSI > `BuyBreakdown`, und aktueller RSI < `BuyRsiValue`.
  - **Verkauf**: Aktueller RSI minus RSI vor `sell_period` Balken ≥ `SellMovement`,
    aktueller RSI minus vorheriger RSI > `SellBreakdown`, und aktueller RSI > `SellRsiValue`.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Fester Take-Profit und Stop-Loss in Ticks.
- **Stops**: Ja, mit `BuyStopLoss`, `BuyTakeProfit`, `SellStopLoss` und `SellTakeProfit`.
- **Filter**:
  - Mindestverzögerung zwischen Trades (`TradeDelaySeconds`).
  - Maximale gleichzeitige offene Trades (`MaxOpenTrades`).
