# Estrategia de Índice de Fuerza de Rebote
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia implementa una versión simplificada del Bounce Strength Index (BSI). El indicador mide cómo el precio cierra dentro de un rango reciente y aplica un doble suavizado para resaltar los cambios de momentum.

## Lógica
- Calcular los precios más altos y más bajos recientes usando los indicadores **Highest** y **Lowest**.
- Determinar la posición del cierre dentro de ese rango y suavizar el resultado dos veces con **SimpleMovingAverage**.
- Cuando el indicador gira hacia arriba, se cierran las posiciones cortas y se abre una posición larga.
- Cuando el indicador gira hacia abajo, se cierran las posiciones largas y se abre una posición corta.

## Parámetros
- `CandleType` – serie de velas utilizada para el análisis.
- `RangePeriod` – período de retrospección para el cálculo del rango.
- `Slowing` – longitud del suavizado rápido.
- `AvgPeriod` – longitud del suavizado lento.

## Indicadores
- BounceStrengthIndex (personalizado)
- Highest
- Lowest
- SimpleMovingAverage
