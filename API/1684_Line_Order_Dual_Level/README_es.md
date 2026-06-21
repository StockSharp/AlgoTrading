# Estrategia de Orden por Niveles Duales
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una adaptación del asesor experto de MetaTrader "MyLineOrder" para la API de StockSharp. Permite al trader definir niveles de precio horizontales que activan órdenes de mercado automáticas cuando el precio los toca. Las distancias opcionales de stop loss, take profit y trailing stop se expresan en pips, y el volumen de la operación es configurable.

Cuando el precio de mercado alcanza el nivel **BuyPrice**, la estrategia entra en una posición larga. Al tocar el nivel **SellPrice** se abre una posición corta. Tras la entrada, la estrategia supervisa la posición y sale cuando se cumple una de las condiciones de protección: stop loss, take profit o trailing stop.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El precio toca o supera `BuyPrice`.
  - **Corto**: El precio toca o cae por debajo de `SellPrice`.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Stop loss, take profit o trailing stop.
- **Stops**:
  - `StopLossPips`, `TakeProfitPips`, `TrailingStopPips`.
- **Filtros**:
  - Ninguno.
- **Parámetros**:
  - `BuyPrice` – nivel para entrada larga.
  - `SellPrice` – nivel para entrada corta.
  - `StopLossPips` – distancia de stop loss en pips.
  - `TakeProfitPips` – distancia de take profit en pips.
  - `TrailingStopPips` – distancia de trailing stop en pips.
  - `TradeVolume` – volumen de la orden.
  - `CandleType` – marco temporal de las velas para la monitorización del precio.
