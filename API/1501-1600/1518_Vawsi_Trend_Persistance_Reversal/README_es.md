# Estrategia de Reversión VAWSI y Persistencia de Tendencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de reversión que combina VAWSI, persistencia de tendencia y ATR para construir un umbral dinámico sobre velas Heikin-Ashi.

## Detalles

- **Criterios de entrada**: El cierre Heikin-Ashi cruza por encima/debajo del umbral dinámico
- **Largo/Corto**: Ambos
- **Criterios de salida**: Cruce opuesto o stops de protección
- **Stops**: Sí, basados en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 15 minute
  - `SlTp` = 5
  - `RsiWeight` = 100
  - `TrendWeight` = 79
  - `AtrWeight` = 20
  - `CombinationMult` = 1
  - `Smoothing` = 3
  - `CycleLength` = 20
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Ambos
  - Indicadores: RSI, ATR
  - Stops: Sí
  - Complejidad: Avanzado
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
