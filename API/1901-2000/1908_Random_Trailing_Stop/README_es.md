# Estrategia de Trailing Stop Aleatorio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia de Trailing Stop Aleatorio abre operaciones aleatorias sesgadas por una media móvil simple y las gestiona usando un trailing stop.

## Detalles

- **Criterios de entrada**: dirección aleatoria con sesgo de SMA
- **Largo/Corto**: Ambos
- **Criterios de salida**: trailing stop
- **Stops**: Sí
- **Valores predeterminados**:
  - `MinStopLevel` = 0.00036
  - `TrailingStep` = 0.00001
  - `SleepMinutes` = 5
  - `SmaPeriod` = 100
  - `Volume` = 0.1
- **Filtros**:
  - Categoría: Experimental
  - Dirección: Ambos
  - Indicadores: SMA
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: 1m
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Alto
