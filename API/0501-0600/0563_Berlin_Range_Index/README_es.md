# Estrategia Berlin Range Index
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Berlin Range Index filtra el Índice de Choppiness estándar con un factor basado en ATR para resaltar las fases de tendencia y de rango. Cuando el índice filtrado cae por debajo de un umbral mínimo, la estrategia abre una posición en la dirección de la vela actual. Las posiciones se cierran cuando el índice indica una fase de rango o una tendencia debilitada.

## Detalles

- **Criterios de entrada**:
  - Índice de rango filtrado por debajo de `ChopMin` y la dirección de la vela define largo o corto.
- **Criterios de salida**:
  - Índice de rango por encima de `ChopMax` o tendencia debilitada.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Length` = 9
  - `ChopMax` = 40
  - `ChopMin` = 10
  - `AtrLength` = 14
  - `LowLookback` = 14
  - `UseNormalized` = true
  - `StdDevLength` = 14
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Choppiness Index, ATR, Standard Deviation
  - Complejidad: Medio
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
