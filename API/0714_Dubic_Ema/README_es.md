# Estrategia Dubic EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera basándose en la posición del cierre relativa a medias móviles exponenciales calculadas sobre máximos y mínimos. Se evita operar durante rangos estrechos y períodos de baja volatilidad. Las posiciones están protegidas con stops basados en ATR, niveles de toma de ganancias y un trailing stop opcional de Parabolic SAR.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Close > EMA(High) y Close > EMA(Low), filtro de rango inactivo, volatilidad suficiente.
  - **Corto**: Close < EMA(High) y Close < EMA(Low), filtro de rango inactivo, volatilidad suficiente.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Parabolic SAR, stop-loss basado en ATR/fijo o toma de ganancias.
- **Stops**: Sí.
- **Filtros**: Filtro de rango y volatilidad.
