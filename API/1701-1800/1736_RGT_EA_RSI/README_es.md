# Estrategia RGT EA RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina el **Índice de Fuerza Relativa (RSI)** con las **Bandas de Bollinger** para identificar movimientos de precio extremos y operar posibles reversiones. Las posiciones se abren cuando el RSI entra en zonas de sobreventa o sobrecompra y el precio cruza las Bandas de Bollinger. Un stop loss y un trailing stop gestionan el riesgo y aseguran las ganancias.

## Cómo Funciona

1. Se calculan el RSI y las Bandas de Bollinger para las velas entrantes.
2. **Comprar** cuando el RSI está por debajo del nivel de sobreventa y el precio de cierre está por debajo de la banda inferior.
3. **Vender** cuando el RSI está por encima del nivel de sobrecompra y el precio de cierre está por encima de la banda superior.
4. Después de la entrada, se coloca un stop loss fijo. Una vez que la posición alcanza la ganancia mínima, el stop loss sigue al precio.

## Parámetros

| Nombre | Descripción |
|--------|-------------|
| `Volume` | Volumen de la orden. |
| `RsiPeriod` | Período de cálculo del RSI. |
| `RsiHigh` | Umbral de sobrecompra del RSI. |
| `RsiLow` | Umbral de sobreventa del RSI. |
| `StopLoss` | Distancia del stop loss inicial en unidades de precio. |
| `TrailingStop` | Distancia del trailing stop en unidades de precio. |
| `MinProfit` | Beneficio mínimo antes de que se active el trailing. |
| `CandleType` | Tipo de vela usada para los cálculos. |

## Notas

- Funciona con cualquier instrumento y marco temporal compatible con StockSharp.
- Usa órdenes de mercado para entradas y salidas.
- El trailing stop se actualiza en cada vela completada.
