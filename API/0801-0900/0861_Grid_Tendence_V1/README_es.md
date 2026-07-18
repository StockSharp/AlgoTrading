# Estrategia Grid Tendence V1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de trading en cuadrícula que reabre o invierte posiciones basándose en pasos de porcentaje de ganancia.

Comienza en largo y cuando la ganancia alcanza el porcentaje especificado cierra y reabre en la misma dirección. Cuando la pérdida alcanza el porcentaje, cierra y abre en la dirección opuesta.

## Detalles

- **Criterios de entrada**: Siempre en el mercado, comenzando en largo. Reabre o invierte cuando la ganancia o pérdida alcanza `Percent`.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Umbral de ganancia o pérdida.
- **Stops**: No.
- **Valores predeterminados**:
  - `Percent` = 1.0
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Grid
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (1m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
