# Estrategia de Scalp RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de scalping usando cambios rápidos del RSI. Convertida del script MetaTrader `scalpen_rsi.mq4`.
La estrategia abre operaciones cuando el RSI cae o sube bruscamente y aplica niveles fijos de toma de ganancias y stop-loss.

## Detalles

- **Criterios de entrada**:
  - **Compra**: Valor RSI hace `buy_period` barras menos RSI actual ≥ `BuyMovement`,
    RSI anterior menos RSI actual > `BuyBreakdown`, y RSI actual < `BuyRsiValue`.
  - **Venta**: RSI actual menos RSI hace `sell_period` barras ≥ `SellMovement`,
    RSI actual menos RSI anterior > `SellBreakdown`, y RSI actual > `SellRsiValue`.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Toma de ganancias y stop-loss fijos en ticks.
- **Stops**: Sí, usando `BuyStopLoss`, `BuyTakeProfit`, `SellStopLoss` y `SellTakeProfit`.
- **Filtros**:
  - Retraso mínimo entre operaciones (`TradeDelaySeconds`).
  - Máximo de operaciones abiertas simultáneamente (`MaxOpenTrades`).
