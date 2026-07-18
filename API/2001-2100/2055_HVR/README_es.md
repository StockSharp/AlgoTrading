# Estrategia de Ratio de Volatilidad Histórica (HVR)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el Ratio de Volatilidad Histórica (HVR). Compara la volatilidad a corto plazo durante 6 barras con la volatilidad a largo plazo durante 100 barras utilizando retornos logarítmicos. Cuando el ratio sube por encima del umbral, el sistema va largo esperando una expansión de la volatilidad. Cuando cae por debajo del umbral, el sistema va corto.

## Detalles

- **Criterios de entrada**:
  - Largo: `HVR > RatioThreshold`
  - Corto: `HVR < RatioThreshold`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Señal contraria
- **Stops**: No
- **Valores predeterminados**:
  - `ShortPeriod` = 6
  - `LongPeriod` = 100
  - `RatioThreshold` = 1.0
  - `CandleType` = `TimeSpan.FromMinutes(15).TimeFrame()`
- **Filtros**:
  - Categoría: Volatilidad
  - Dirección: Ambos
  - Indicadores: Volatilidad histórica (corta y larga)
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
