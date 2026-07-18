# Estrategia DSS Bressert
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia usa el indicador Double Smoothed Stochastic (DSS) Bressert. Se calculan dos líneas:

- **Línea DSS** – valor estocástico suavizado dos veces con media móvil exponencial.
- **Línea MIT** – valor intermedio después del primer suavizado.

Una operación se abre cuando estas líneas se cruzan:

- Comprar cuando la línea DSS cruza por debajo de la línea MIT después de haber estado por encima.
- Vender cuando la línea MIT cruza por debajo de la línea DSS después de haber estado por encima.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `EmaPeriod` | Período de suavizado EMA (predeterminado: 8) |
| `StoPeriod` | Período de cálculo del estocástico (predeterminado: 13) |
| `TakeProfitPercent` | Porcentaje de take profit para órdenes de protección (predeterminado: 2) |
| `StopLossPercent` | Porcentaje de stop loss para órdenes de protección (predeterminado: 1) |
| `CandleType` | Marco temporal usado para los cálculos (predeterminado: 4 horas) |

## Notas

- La estrategia funciona solo en velas cerradas.
- La protección utiliza stop loss y take profit basados en porcentaje.
