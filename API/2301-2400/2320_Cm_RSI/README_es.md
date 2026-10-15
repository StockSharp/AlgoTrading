# Estrategia cm RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia es un port directo del experto de MetaTrader 4 "cm_RSI". Utiliza el indicador de Fuerza Relativa (RSI) para detectar reversiones de momentum.

El algoritmo monitorea los valores del RSI calculados a partir de los precios de apertura de las velas. Una posición larga se abre cuando el RSI sube por encima de un *nivel de compra* configurable después de estar por debajo. Una posición corta se abre cuando el RSI cae por debajo de un *nivel de venta* configurable después de estar por encima. Cada operación está protegida por valores fijos de take profit y stop loss expresados en puntos de precio.

## Lógica de la estrategia

1. Calcular el RSI con un período definido por el usuario usando los precios de apertura de las velas.
2. Si el valor anterior del RSI estaba por debajo del nivel de compra y el valor actual cruza por encima, abrir una posición larga a mercado.
3. Si el valor anterior del RSI estaba por encima del nivel de venta y el valor actual cruza por debajo, abrir una posición corta a mercado.
4. Cada operación usa el mismo volumen configurable y está protegida con órdenes de stop loss y take profit.

## Parámetros

| Nombre | Descripción |
|--------|-------------|
| `RsiPeriod` | Período de cálculo del RSI. |
| `BuyLevel` | Nivel de RSI utilizado para disparar entradas largas. |
| `SellLevel` | Nivel de RSI utilizado para disparar entradas cortas. |
| `TakeProfit` | Take profit en puntos de precio absolutos. |
| `StopLoss` | Stop loss en puntos de precio absolutos. |
| `OrderVolume` | Volumen aplicado a cada operación. |
| `CandleType` | Tipo de velas utilizadas para los cálculos. |

## Notas

- La estrategia procesa solo velas terminadas.
- Mantiene una única posición abierta en cualquier momento.
- `StartProtection` se utiliza para gestionar automáticamente las órdenes de stop loss y take profit.

