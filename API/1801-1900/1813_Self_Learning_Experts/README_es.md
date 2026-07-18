# Estrategia de Expertos con Autoaprendizaje
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia aprende de patrones históricos binarios de precios y estima la probabilidad de movimiento futuro al alza o a la baja. Cuando la probabilidad supera un umbral definido por el usuario, la estrategia abre una posición de mercado en esa dirección. Las estadísticas recopiladas decaen con el tiempo mediante un factor de olvido para dar más peso al comportamiento reciente. El sistema puede opcionalmente mover los niveles de stop cuando aparecen nuevas señales y soporta un stop trailing basado en pasos de precio.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Probabilidad de movimiento al alza ≥ `ProbabilityThreshold`.
  - **Corto**: Probabilidad de movimiento a la baja ≥ `ProbabilityThreshold`.
- **Stops**: Stop trailing opcional con stop-loss y take-profit simétricos.
- **Valores predeterminados**:
  - `PatternSize` = 10
  - `ProbabilityThreshold` = 0.8
  - `ForgetRate` = 1.05
  - `Trailing` = 0 (desactivado)
- **Filtros**:
  - Categoría: Reconocimiento de patrones
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Opcional
  - Complejidad: Alto
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Alto
