# Estrategia BullBear Volumen Percentil TP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza Bull/Bear Power normalizado mediante un Z-Score.
Las posiciones largas se abren cuando el Z-Score cruza por encima del umbral,
mientras que las posiciones cortas se abren cuando cruza por debajo del umbral negativo.
Los niveles de toma de ganancias se basan en multiplicadores de ATR ajustados por volumen y percentiles de precio.

## Detalles

- **Criterios de entrada:**
  - **Largo**: Z-Score cruza por encima de `ZThreshold`.
  - **Corto**: Z-Score cruza por debajo de `-ZThreshold`.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Z-Score cruza de vuelta a través de cero o alcanza niveles de toma de ganancias.
- **Stops**: Toma de ganancias mediante multiplicadores de ATR.
- **Valores predeterminados:**
  - Longitud EMA 21, longitud Z-Score 252, umbral 1.618.
  - Período ATR 20, multiplicadores 1.618 / 2.382 / 3.618.
  - Período MA de volumen 100, período de percentil 100.
- **Filtros:**
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: EMA, ATR
  - Stops: Sí
  - Complejidad: Medio
  - Marco temporal: Medio plazo
