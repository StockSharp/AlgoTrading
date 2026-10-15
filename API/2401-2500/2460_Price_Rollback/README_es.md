# Estrategia de Retroceso de Precio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera brechas de precio diarias.
Al comienzo de un día de la semana seleccionado, compara el último precio de cierre con el precio de apertura 24 horas antes.
Si la brecha es mayor que el parámetro **Corridor**, abre una posición en la dirección del retroceso:

- Brecha al alza → vender.
- Brecha a la baja → comprar.

Las operaciones utilizan stop loss y take profit fijos en unidades de precio.
Se aplica un trailing stop con paso después de que la posición avanza en ganancias.
Todas las posiciones se cierran al final del día (22:45).

## Parámetros
- `Corridor` – umbral de la brecha.
- `StopLoss` – distancia de stop loss fija.
- `TakeProfit` – objetivo de take profit fijo.
- `TrailingStop` – distancia del trailing stop.
- `TrailingStep` – movimiento necesario para actualizar el trailing.
- `TradingDay` – día de la semana para abrir operaciones (0=domingo).
- `CandleType` – marco temporal para los cálculos.
