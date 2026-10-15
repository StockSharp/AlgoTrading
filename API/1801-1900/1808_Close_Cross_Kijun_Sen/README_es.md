# Estrategia de Cierre por Cruce de Kijun-Sen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia actúa como herramienta de gestión de operaciones. Cierra las posiciones existentes cuando el precio de cierre cruza la línea Kijun-sen del indicador Ichimoku.

Durante la ejecución, la estrategia se suscribe a las velas y calcula el valor de Kijun-sen. Cuando hay una posición larga y el precio cae por debajo de la línea Kijun por un desplazamiento configurable, la posición se cierra. Cuando hay una posición corta abierta y el precio sube por encima de la línea, la posición también se cierra. La estrategia no abre nuevas operaciones.

## Detalles

- **Criterios de entrada**: La estrategia no abre nuevas operaciones; solo gestiona las posiciones existentes.
- **Largo/Corto**: Ambos (cierre).
- **Criterios de salida**: Precio de cierre cruzando la línea Kijun-sen por el desplazamiento especificado.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `KijunPeriod` = 50
  - `PointsToCross` = 0
  - `CandleType` = 5 minutos
- **Filtros**:
  - Categoría: Gestión de operaciones
  - Dirección: Ambos
  - Indicadores: Ichimoku
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
