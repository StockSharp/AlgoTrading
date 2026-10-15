# Estrategia Spread By
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Spread By utiliza una media móvil con bandas de desviación estándar para operar en extremos de precio.
Compra cuando el precio cae por debajo de la banda inferior y vende cuando el precio sube por encima de la banda superior.

## Detalles

- **Criterios de entrada**: el precio se desplaza más allá de ±1 desviación estándar de la media móvil
- **Largo/Corto**: Ambos
- **Criterios de salida**: el precio regresa a la media móvil
- **Stops**: No
- **Valores predeterminados**:
  - `Length` = 100
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: SMA, StandardDeviation
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
