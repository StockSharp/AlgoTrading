# Estrategia Auto Trailing Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Adjunta automáticamente órdenes de stop-loss y take-profit a posiciones existentes y mueve el stop a medida que el precio se mueve a favor.

## Detalles
- **Criterios de entrada**: Ninguno, la estrategia no abre operaciones.
- **Largo/Corto**: Funciona con posiciones largas y cortas ya abiertas.
- **Criterios de salida**: Órdenes de stop-loss y take-profit. El trailing stop se actualiza después de que el precio se mueve la mitad de la distancia de trailing.
- **Stops**: Stop-loss y take-profit iniciales colocados cuando aparece la posición; el stop-loss sigue mediante `TrailingStopStep`.
- **Valores predeterminados**: TrailingStop 6, TrailingStopStep 1, TakeProfit 35, StopLoss 114.
- **Filtros**: Desactivación opcional del trailing stop, take profit automático o stop loss automático mediante parámetros.

## Parámetros
- `FridayTrade` - permitir trailing los viernes.
- `UseTrailingStop` - activar la lógica de trailing stop.
- `AutoTrailingStop` - usar la distancia de trailing predeterminada de 6 cuando es verdadero.
- `TrailingStop` - distancia de trailing en unidades de precio cuando AutoTrailingStop es falso.
- `TrailingStopStep` - movimiento mínimo de precio antes de mover el trailing stop.
- `AutomaticTakeProfit` - colocar automáticamente una orden de take profit.
- `TakeProfit` - distancia del take profit.
- `AutomaticStopLoss` - colocar automáticamente una orden de stop loss.
- `StopLoss` - distancia del stop loss.
- `CandleType` - tipo de vela para actualizaciones de precio (predeterminado 1 minuto).
