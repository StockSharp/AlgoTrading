# Estrategia Simple de Retroceso de Fibonacci
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Utiliza niveles de retroceso de Fibonacci derivados del máximo más alto y el mínimo más bajo durante una ventana de retrospección. Cuando el precio cruza un nivel de Fibonacci seleccionado, la estrategia entra en una posición y coloca órdenes de take profit y stop loss fijos basados en pips.

## Detalles

- **Entrada**: Cruce por encima o por debajo del nivel de Fibonacci elegido.
- **Salida**: Take profit o stop loss fijo.
- **Indicadores**: Highest, Lowest.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `LookbackPeriod` = 100
  - `TakeProfitPips` = 50
  - `StopLossPips` = 20
- **Dirección**: Ambos.
