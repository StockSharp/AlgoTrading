# Estrategia de Movimiento Dirigido
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia replica el asesor experto **Directed Movement** de MetaTrader. Aplica un Índice de Fuerza Relativa (RSI) que se suaviza dos veces mediante medias móviles. El primer suavizado forma una línea rápida mientras que el segundo suavizado crea una línea más lenta.

Las decisiones de trading se basan en el cruce de las líneas rápida y lenta de forma contraria:

- **Comprar** cuando la línea rápida cruza por debajo de la línea lenta.
- **Vender** cuando la línea rápida cruza por encima de la línea lenta.

Los niveles opcionales de stop-loss y take-profit se aplican como porcentajes del precio de entrada.

## Indicadores

- `RelativeStrengthIndex` – indicador de momentum base.
- `MovingAverage` – primer suavizado del RSI (línea rápida).
- `MovingAverage` – segundo suavizado de la línea rápida (línea lenta).

## Reglas de trading

1. Calcular el RSI a partir de los cierres de las velas.
2. Suavizar el RSI con la primera media móvil para obtener la línea rápida.
3. Suavizar la línea rápida con la segunda media móvil para obtener la línea lenta.
4. Entrar en una posición larga cuando la línea rápida cruza por debajo de la línea lenta. Cerrar cualquier posición corta antes de abrir la nueva larga.
5. Entrar en una posición corta cuando la línea rápida cruza por encima de la línea lenta. Cerrar cualquier posición larga antes de abrir la nueva corta.
6. Aplicar protecciones de stop-loss y take-profit si sus parámetros son mayores que cero.

## Parámetros

| Nombre | Descripción |
|--------|-------------|
| `CandleType` | Serie de velas utilizada para los cálculos. |
| `RsiPeriod` | Período de cálculo del RSI. |
| `FirstMaType` | Tipo de media móvil utilizada para la línea rápida. |
| `FirstMaLength` | Período de la media móvil rápida. |
| `SecondMaType` | Tipo de media móvil utilizada para la línea lenta. |
| `SecondMaLength` | Período de la media móvil lenta. |
| `StopLossPercent` | Stop-loss en porcentaje del precio de entrada. |
| `TakeProfitPercent` | Take-profit en porcentaje del precio de entrada. |

