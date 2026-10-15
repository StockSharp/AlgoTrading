# Estrategia Cidomo V1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de ruptura diaria que coloca operaciones cuando el precio escapa del rango reciente.

## Resumen

- **Tipo**: Ruptura
- **Entrada**: Compra cuando el precio rompe por encima del máximo más alto del período de retrospectiva, vende cuando el precio rompe por debajo del mínimo más bajo.
- **Salida**: Stop loss, take profit, breakeven y trailing stop opcionales.
- **Indicadores**: Highest, Lowest

## Parámetros

| Nombre | Descripción |
|--------|-------------|
| `Lookback` | Número de velas utilizadas para calcular el rango. |
| `Delta` | Desplazamiento de precio añadido a los niveles de ruptura. |
| `StopLoss` | Stop loss en puntos de precio. |
| `TakeProfit` | Take profit en puntos de precio. |
| `NoLoss` | Mover el stop al nivel de entrada después de esta ganancia (en puntos). |
| `Trailing` | Distancia de trailing en puntos. |
| `UseTimeFilter` | Si es verdadero, los niveles se calculan después de la hora especificada. |
| `TradeTime` | Hora del día para calcular los niveles de ruptura. |
| `CandleType` | Tipo de vela utilizado para los cálculos. |

## Notas

La estrategia monitorea únicamente velas completadas. Los niveles se recalculan una vez al día después de `TradeTime`.
