# Modelo Dinámico de Oscilador de Ticks (DTOM)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El **Dynamic Ticks Oscillator Model** utiliza la tasa de cambio del índice NYSE Down Ticks. Cuando el ROC cae por debajo de un umbral dinámico basado en la desviación estándar, la estrategia abre una posición larga. La posición se cierra una vez que el ROC sube por encima de un umbral positivo.

## Detalles
- **Criterios de entrada**: `ROC < -StdDev * EntryStdDevMultiplier`
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: `ROC > StdDev * ExitStdDevMultiplier`
- **Stops**: No.
- **Valores predeterminados**:
  - `RocLength = 5`
  - `VolatilityLookback = 24`
  - `EntryStdDevMultiplier = 1.6m`
  - `ExitStdDevMultiplier = 1.4m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Solo largos
  - Indicadores: RateOfChange, StandardDeviation
  - Stops: No
  - Complejidad: Principiante
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
