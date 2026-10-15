# Estrategia Hedge Average
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia reproduce el experto "Hedge Average" de MetaTrader. Compara medias móviles simples de los precios de apertura y cierre en dos períodos de tiempo.

## Lógica de trading

- Calcular la SMA del precio de apertura y cierre para `Period1` y `Period2`.
- Si la media de apertura del período largo está por encima de su media de cierre **y** la media de apertura del período corto está por debajo de su media de cierre, se abre una posición larga.
- Si la media de apertura del período largo está por debajo de su media de cierre **y** la media de apertura del período corto está por encima de su media de cierre, se abre una posición corta.
- El trading solo está permitido entre `StartHour` y `EndHour`.
- El stop-loss y el take-profit opcionales se establecen en unidades de precio absolutas. El trailing stop mueve el stop protector junto con el precio cuando está habilitado.

## Parámetros

- `Period1` – período para las medias rápidas.
- `Period2` – período para las medias lentas.
- `StartHour` – hora del día en que el trading se activa.
- `EndHour` – hora del día en que el trading se detiene.
- `CandleType` – marco temporal de velas utilizado para los cálculos.
- `TakeProfit` – distancia del take profit en unidades de precio.
- `StopLoss` – distancia del stop loss en unidades de precio.
- `UseTrailing` – activar trailing stop basado en la distancia del stop-loss.

## Notas

La estrategia utiliza un enfoque de posición única y no replica el objetivo de ganancia basado en dinero de la versión MQL original.
