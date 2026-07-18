# Estrategia Z-Strike Recovery
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Entra en largo cuando el z-score del cambio de precio supera un umbral y sale tras un número fijo de barras.

## Detalles

- **Criterios de entrada**: Z-score del cambio de precio > umbral
- **Largo/Corto**: Solo largos
- **Criterios de salida**: Salida por tiempo
- **Stops**: No
- **Valores predeterminados**:
  - `ZLength` = 16
  - `ZThreshold` = 1.3
  - `ExitPeriods` = 10
- **Filtros**:
  - Categoría: Estadístico
  - Dirección: Largo
  - Indicadores: SMA, StandardDeviation
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
