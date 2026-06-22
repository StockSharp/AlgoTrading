# Estrategia Panel Joke
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia convierte el sistema original *panel-joke* de MetaTrader a StockSharp. Compara la vela actual con la anterior en siete métricas de precio (apertura, máximo, mínimo, promedio de máximo y mínimo, cierre, promedio de máximo/mínimo/cierre y promedio ponderado de máximo/mínimo/cierre). Cada métrica que aumentó cuenta hacia una configuración larga potencial; cada disminución cuenta hacia una configuración corta.

Cuando el parámetro `Enable Autopilot` es `true`, la estrategia abre o invierte posiciones automáticamente según qué lado tenga más puntos. No se utilizan indicadores adicionales ni reglas de stop.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Buy counter > Sell counter.
  - **Corto**: Sell counter > Buy counter.
- **Criterios de salida**: Invertir cuando aparece la señal opuesta.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Enable Autopilot` = `true`.
  - `Candle Type` = Marco temporal de 5 minutos.
- **Filtros**:
  - Categoría: Price action
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Alto

