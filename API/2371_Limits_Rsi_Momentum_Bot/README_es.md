# Estrategia Limits RSI Momentum Bot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Resumen
Esta estrategia coloca órdenes límite basadas en los indicadores de Índice de Fuerza Relativa (RSI) y Momentum. Su objetivo es comprar con descuentos y vender con primas utilizando órdenes pendientes en lugar de ejecuciones de mercado.

## Reglas de Trading
- Opera solo durante la ventana de tiempo especificada.
- En cada vela finalizada, se calculan los valores de RSI y Momentum.
- Se coloca una **orden límite de compra** por debajo de la apertura de la vela cuando RSI y Momentum están ambos por debajo de sus umbrales de compra.
- Se coloca una **orden límite de venta** por encima de la apertura de la vela cuando RSI y Momentum están ambos por encima de sus umbrales de venta.
- Cuando se abre una posición, la orden pendiente opuesta se cancela.
- El stop-loss y el take-profit se gestionan automáticamente mediante `StartProtection`.

## Parámetros
- `Volume` – volumen de la orden.
- `LimitOrderDistance` – distancia en pasos de precio desde la apertura de la vela para colocar órdenes pendientes.
- `TakeProfit` – objetivo de beneficio en pasos de precio.
- `StopLoss` – límite de pérdida en pasos de precio.
- `RsiPeriod` – período para el cálculo del RSI.
- `RsiBuyRestrict` / `RsiSellRestrict` – umbrales de RSI que permiten entradas largas o cortas.
- `MomentumPeriod` – período para el cálculo del Momentum.
- `MomentumBuyRestrict` / `MomentumSellRestrict` – umbrales de Momentum para entradas largas o cortas.
- `StartTime` / `EndTime` – límites de la sesión de trading.
- `CandleType` – intervalo de vela utilizado para los cálculos de indicadores.

## Notas
La estrategia está convertida del script MQL4 "The Limits Bot with RSI & Momentum" y utiliza la API de alto nivel de StockSharp.
