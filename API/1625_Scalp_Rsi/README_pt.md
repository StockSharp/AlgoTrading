# Estratégia de Scalp RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de scalping usando mudanças rápidas do RSI. Convertida do script MetaTrader `scalpen_rsi.mq4`.
A estratégia abre operações quando o RSI cai ou sobe bruscamente e aplica níveis fixos de take profit e stop loss.

## Detalhes

- **Critérios de entrada**:
  - **Compra**: Valor RSI de `buy_period` barras atrás menos RSI atual ≥ `BuyMovement`,
    RSI anterior menos RSI atual > `BuyBreakdown`, e RSI atual < `BuyRsiValue`.
  - **Venda**: RSI atual menos RSI de `sell_period` barras atrás ≥ `SellMovement`,
    RSI atual menos RSI anterior > `SellBreakdown`, e RSI atual > `SellRsiValue`.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Take profit e stop loss fixos em ticks.
- **Stops**: Sim, usando `BuyStopLoss`, `BuyTakeProfit`, `SellStopLoss` e `SellTakeProfit`.
- **Filtros**:
  - Atraso mínimo entre operações (`TradeDelaySeconds`).
  - Máximo de operações abertas simultaneamente (`MaxOpenTrades`).
