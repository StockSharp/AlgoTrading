# Estrategia Hans123 Trader v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Hans123 Trader v2 es una estrategia de rompimiento que coloca órdenes de stop pendientes alrededor del rango de trading reciente. Refleja la implementación de MetaTrader por Vladimir Karputov y está adaptada a la API de alto nivel de StockSharp. El sistema se enfoca en capturar momentum cuando el precio escapa del rango más reciente de 80 barras mientras gestiona salidas protectoras y un trailing stop.

## Idea central

- Monitorear una serie de velas configurable (barras de 1 hora por defecto).
- Durante la ventana de sesión activa, calcular el máximo más alto y el mínimo más bajo sobre las últimas *N* velas (80 por defecto).
- Colocar una orden de buy stop en el máximo más alto y una orden de sell stop en el mínimo más bajo cuando el mercado esté lo suficientemente lejos del bid/ask actual.
- Limitar el número total de órdenes pendientes activas para evitar sobreexposición.
- Una vez que se abre una posición, cancelar las órdenes pendientes restantes, aplicar offsets de stop-loss y take-profit (medidos en pips), y activar un trailing stop.

## Gestión de operaciones

- **Entradas**: Las órdenes de stop se colocan solo mientras el tiempo de la vela procesada cae entre las horas de inicio y fin configuradas. Las órdenes se ignoran fuera de esa ventana.
- **Protección de posición**: Cuando se crea una nueva posición, la estrategia registra inmediatamente órdenes de stop-loss y take-profit protectoras usando las distancias de pip configuradas.
- **Trailing stop**: Si está habilitado, la orden de stop-loss se vuelve a emitir más cerca del precio una vez que se mueve a favor de la posición más que el umbral de trailing más el paso.
- **Limpieza de órdenes**: Salir de una posición cancela las órdenes protectoras, y cualquier nueva entrada cancela las órdenes pendientes opuestas, coincidiendo con el comportamiento de la lógica MQL original.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `Volume` | Tamaño de orden usado al enviar órdenes de rompimiento y protectoras. |
| `StopLossPips` | Distancia en pips entre el precio de entrada y el stop-loss protector. Establecer en `0` para deshabilitar. |
| `TakeProfitPips` | Distancia en pips entre el precio de entrada y la orden de take-profit. Establecer en `0` para deshabilitar. |
| `TrailingStopPips` | Distancia inicial del trailing stop en pips. `0` deshabilita el trailing. |
| `TrailingStepPips` | Beneficio adicional mínimo en pips requerido antes de mover el trailing stop de nuevo. Debe ser diferente de cero cuando el trailing está habilitado. |
| `StartHour` | Hora de apertura de sesión (inclusive) para colocar nuevas órdenes pendientes. |
| `EndHour` | Hora de cierre de sesión (exclusive) para colocar nuevas órdenes pendientes. Debe ser mayor que `StartHour`. |
| `MaxPendingOrders` | Número máximo de órdenes de rompimiento simultáneas (compra + venta) permitidas. |
| `BreakoutPeriod` | Longitud de retroceso (en velas) para los cálculos de máximo más alto y mínimo más bajo. |
| `CandleType` | Serie de velas procesada por la estrategia (marco temporal u otro tipo de datos de velas). |

## Notas

- El tamaño del pip se deriva del paso de precio del instrumento. Para símbolos forex de 3 y 5 dígitos, el valor del punto se ajusta para coincidir con la definición MQL de un pip.
- La estrategia depende de las instantáneas `Security.BestBid`/`BestAsk` cuando están disponibles. Si no hay datos de profundidad, recurre al precio de cierre de vela actual para evaluar la distancia mínima del mercado.
- Las órdenes protectoras se recrean cada vez que necesitan moverse, reflejando la lógica `PositionModify` del expert advisor original.
- La implementación mantiene la lógica puramente en C# sin traducción a Python, como se solicitó.
