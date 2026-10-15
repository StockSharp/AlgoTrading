# Estrategia de Coeficiente de Correlación de Rango de Spearman
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia de trading en pares mide la correlación de rango de Spearman entre dos valores. Cuando la correlación supera un umbral positivo, la estrategia va corta en el primer valor y larga en el segundo. Cuando cae por debajo del umbral negativo toma la posición opuesta. Las posiciones se cierran cuando la correlación vuelve hacia cero.

## Detalles

- **Criterios de entrada:**
  - **Largo primero / Corto segundo**: correlación < -Threshold.
  - **Corto primero / Largo segundo**: correlación > Threshold.
- **Largo/Corto**: Trading en pares.
- **Criterios de salida:**
  - Valor absoluto de la correlación < Threshold / 2.
- **Stops**: No.
- **Valores predeterminados:**
  - `CorrelationPeriod` = 10
  - `Threshold` = 0.8
- **Filtros:**
  - Categoría: Correlación
  - Dirección: Ambos
  - Indicadores: Spearman Rank Correlation
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
