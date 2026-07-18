# Estrategia de Barras Consecutivas por Encima de la MA Solo Corto
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia solo corto que cuenta los cierres consecutivos por encima de una media móvil y vende en rupturas por encima del máximo anterior. Sale cuando el precio cae por debajo del mínimo anterior. Un filtro EMA 200 opcional impone la tendencia bajista.

## Detalles

- **Criterios de entrada**: Umbral de cierres consecutivos por encima de la MA y cierre > máximo anterior
- **Largo/Corto**: Corto
- **Criterios de salida**: Cierre por debajo del mínimo anterior
- **Stops**: No
- **Valores predeterminados**:
  - `Threshold` = 3
  - `MaType` = SMA
  - `MaLength` = 5
  - `EmaPeriod` = 200
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Corto
  - Indicadores: MA, EMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
