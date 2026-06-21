# Estrategia Detectora de Valores Atípicos con Intervalos de Confianza N-Sigma
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia identifica valores atípicos en los cambios de precio mediante intervalos de confianza N-sigma y opera reversión a la media cuando ocurren movimientos extremos.

## Detalles

- **Criterios de entrada**:
  - Corto cuando el z-score > `SecondLimit`.
  - Largo cuando el z-score < -`SecondLimit`.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Cerrar la posición cuando |z-score| < `FirstLimit`.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `SampleSize` = 30
  - `FirstLimit` = 2
  - `SecondLimit` = 3
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: StandardDeviation, Z-Score
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Nivel de riesgo: Medio
