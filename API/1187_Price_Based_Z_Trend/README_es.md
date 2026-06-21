# Estrategia de Price Based Z-Trend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Opera basándose en el Z-score del precio relativo a una EMA. Entra cuando el Z-score cruza umbrales definidos por el usuario y soporta direcciones largas, cortas o ambas.

## Detalles

- **Criterios de entrada**:
  - Z-score cruza por encima de `Threshold` para largo.
  - Z-score cruza por debajo de `-Threshold` para corto.
- **Largo/Corto**: Configurable mediante `TradeDirection`.
- **Criterios de salida**: Cruce del umbral opuesto.
- **Stops**: No.
- **Valores predeterminados**:
  - `PriceDeviationLength` = 100
  - `PriceAverageLength` = 100
  - `Threshold` = 1
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Configurable
  - Indicadores: EMA, StandardDeviation
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: 5 minutos
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
