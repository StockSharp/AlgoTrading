# Estrategia BuySell
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia emula el experto **BuySell** de MetaTrader. Combina una media móvil con el Average True Range (ATR) para detectar reversiones de tendencia.
Cuando la media móvil se orienta hacia arriba, el sistema considera el mercado alcista; cuando se orienta hacia abajo, lo considera bajista.
Una operación se abre solo si la vela anterior estaba en el estado contrario, confirmando una reversión. Los niveles opcionales de stop-loss y take-profit se expresan en puntos de precio.

## Detalles

- **Lógica de entrada**
  - **Largo**: la media móvil pasa de caer a subir y la vela anterior era bajista.
  - **Corto**: la media móvil pasa de subir a caer y la vela anterior era alcista.
- **Lógica de salida**
  - **Largo**: la media móvil se gira a la baja o se activa el stop-loss / take-profit.
  - **Corto**: la media móvil se gira al alza o se activa el stop-loss / take-profit.
- **Indicadores**: Media Móvil Simple (SMA) y ATR.
- **Stops**: Stop-loss y take-profit en puntos.
- **Permisos**: indicadores separados permiten o prohíben abrir/cerrar posiciones largas y cortas.
- **Marco temporal predeterminado**: velas de 4 horas.

## Parámetros

| Nombre | Predeterminado | Descripción |
| ------ | -------------- | ----------- |
| `MaPeriod` | 14 | Período de la media móvil. |
| `AtrPeriod` | 60 | Período del ATR. |
| `StopLoss` | 1000 | Stop-loss en puntos de precio. |
| `TakeProfit` | 2000 | Take-profit en puntos de precio. |
| `AllowLongEntry` | true | Permiso para abrir posiciones largas. |
| `AllowShortEntry` | true | Permiso para abrir posiciones cortas. |
| `AllowLongExit` | true | Permiso para cerrar posiciones largas. |
| `AllowShortExit` | true | Permiso para cerrar posiciones cortas. |
| `CandleType` | H4 | Marco temporal utilizado para los cálculos. |

## Uso

1. Añade la estrategia a tu solución StockSharp.
2. Configura los parámetros según sea necesario.
3. Ejecuta la estrategia en modo en vivo o de backtesting. Las operaciones se ejecutan mediante órdenes `BuyMarket` y `SellMarket`.

El enfoque es adecuado para mercados donde las reversiones de tendencia van acompañadas de cambios de volatilidad capturados por el ATR.
