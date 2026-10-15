# Estrategia de Reversión a la Media Consecutive Close High1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia solo en corto que cuenta los cierres consecutivos por encima del máximo anterior y vende una vez que el conteo alcanza un umbral. La posición se cierra cuando el precio cae por debajo del mínimo anterior. El filtro EMA 200 opcional confirma la tendencia bajista.

## Detalles

- **Criterios de entrada**: los cierres consecutivos por encima del máximo anterior alcanzan el umbral
- **Largo/Corto**: Corto
- **Criterios de salida**: cierre por debajo del mínimo anterior
- **Stops**: No
- **Valores predeterminados**:
  - `Threshold` = 3
  - `EmaPeriod` = 200
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Corto
  - Indicadores: EMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
