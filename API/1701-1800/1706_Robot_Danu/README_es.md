# Estrategia Robot Danu
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que compara los niveles rápidos y lentos de ZigZag derivados de los máximos y mínimos de las velas.
Se abre una posición corta cuando el nivel ZigZag rápido está por encima del lento.
Se abre una posición larga cuando el nivel ZigZag rápido está por debajo del lento.

## Detalles
- **Criterios de entrada**: Comparación de pivotes ZigZag rápidos y lentos.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: Relación ZigZag opuesta.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `FastLength` = 28
  - `SlowLength` = 56
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Highest, Lowest
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
