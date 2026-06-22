# Estrategia CM Fishing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia CM Fishing** es un enfoque de trading en cuadrícula adaptado del script MQL original `cm_fishing.mq4`. La estrategia abre órdenes de mercado siempre que el precio se mueve un número fijo de puntos desde la última operación ejecutada. Puede construir una cuadrícula de posiciones largas o cortas y cerrarlas cuando se alcanza un objetivo de beneficio especificado.

Esta implementación se centra en la lógica central de trading sin la interfaz gráfica del script original. Las órdenes se ejecutan usando la API de alto nivel de StockSharp.

## Parámetros

| Nombre | Descripción |
|--------|-------------|
| `Buy` | Habilita o deshabilita la apertura de posiciones largas. |
| `Sell` | Habilita o deshabilita la apertura de posiciones cortas. |
| `StepBuy` | Paso de precio en puntos que debe pasarse hacia abajo antes de abrir una nueva posición larga. |
| `StepSell` | Paso de precio en puntos que debe pasarse hacia arriba antes de abrir una nueva posición corta. |
| `CloseProfitBuy` | Umbral de beneficio para cerrar todas las posiciones largas. |
| `CloseProfitSell` | Umbral de beneficio para cerrar todas las posiciones cortas. |
| `CloseProfit` | Umbral de beneficio que cierra cualquier posición abierta independientemente de la dirección. |
| `BuyVolume` | Volumen de la orden para cada operación larga. |
| `SellVolume` | Volumen de la orden para cada operación corta. |

## Lógica de trading

1. Rastrear los precios de las operaciones en tiempo real.
2. Cuando el precio cae `StepBuy` desde el último nivel de operación y `Buy` está habilitado, enviar una orden de compra de mercado.
3. Cuando el precio sube `StepSell` desde el último nivel de operación y `Sell` está habilitado, enviar una orden de venta de mercado.
4. Mantener el precio de entrada promedio de la posición actual.
5. Cerrar posiciones cuando el beneficio no realizado supere el parámetro `CloseProfit*` correspondiente.

La estrategia trabaja con datos de ticks y es adecuada para fines de demostración y educativos.

## Notas

- La implementación no reproduce la interfaz de usuario del script original.
- Solo se mantiene una posición neta (larga o corta) en cualquier momento.
