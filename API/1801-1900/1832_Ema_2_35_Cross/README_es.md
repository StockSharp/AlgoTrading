# Estrategia de Cruce EMA 2-35
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia sigue un simple cruce entre dos Medias Móviles Exponenciales. La EMA rápida con longitud 2 reacciona rápidamente a los cambios de precio, mientras que la EMA lenta con longitud 35 representa la tendencia a largo plazo. Se abre una posición cuando la EMA rápida cruza la EMA lenta; las posiciones se invierten cuando ocurre el cruce opuesto.

La gestión del riesgo se maneja con niveles fijos de stop-loss y take-profit expresados en pasos de precio. También se aplica un trailing stop para asegurar ganancias a medida que la operación avanza en una dirección favorable.

## Detalles

- **Criterios de entrada**:
  - **Largo**: EMA(2) cruza por encima de EMA(35).
  - **Corto**: EMA(2) cruza por debajo de EMA(35).
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Cruce opuesto.
  - Stop-loss o take-profit alcanzado.
  - Trailing stop activado.
- **Stops**: Stop-loss fijo, take-profit y trailing stop (todos en pasos de precio).
- **Valores predeterminados**:
  - `FastLength` = 2
  - `SlowLength` = 35
  - `StopLoss` = 50
  - `TakeProfit` = 150
  - `TrailingStop` = 50
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Medias móviles
  - Stops: Sí
  - Complejidad: Simple
  - Marco temporal: Corto plazo

