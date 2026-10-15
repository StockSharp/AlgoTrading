# Estrategia RSI Trader V1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza el Índice de Fuerza Relativa (RSI) para identificar reversiones tras extremos a corto plazo. Una señal de compra ocurre cuando el RSI cruza por encima del umbral de sobreventa después de permanecer por debajo durante dos velas consecutivas. Una señal de venta ocurre cuando el RSI cruza por debajo del umbral de sobrecompra después de permanecer por encima durante dos velas. La estrategia opcionalmente cierra una posición opuesta existente y opera solo dentro de una ventana de tiempo configurable.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `RSI > BuyPoint` y el RSI de las dos velas anteriores `< BuyPoint`.
  - **Corto**: `RSI < SellPoint` y el RSI de las dos velas anteriores `> SellPoint`.
- **Criterios de salida**: Señal opuesta o stop/take-profit de protección.
- **Filtro de tiempo**: Opera solo cuando la hora de apertura de la vela está entre `StartHour` y `EndHour`.
- **Stops**: Take profit y stop loss fijos expresados en unidades de precio.
- **Parámetros**:
  - `RsiPeriod` – período de cálculo del RSI.
  - `BuyPoint` – nivel de sobreventa para entradas largas.
  - `SellPoint` – nivel de sobrecompra para entradas cortas.
  - `CloseOnOpposite` – cerrar la posición actual cuando aparece una señal opuesta.
  - `StartHour` / `EndHour` – horas de trading.
  - `TakeProfit` / `StopLoss` – niveles de protección en precio.

Este ejemplo demuestra un sistema minimalista de cruce RSI construido con la API de alto nivel de StockSharp. Puede utilizarse como plantilla para mayor experimentación.
