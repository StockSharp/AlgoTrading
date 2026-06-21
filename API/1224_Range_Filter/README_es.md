# Estrategia de Filtro de Rango
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de filtro de rango con cálculo de rango realista y niveles fijos de riesgo/recompensa.

Utiliza un rango suavizado para crear bandas dinámicas alrededor del precio. Las operaciones se toman cuando el precio rompe por encima o por debajo de estas bandas. La gestión del riesgo utiliza distancias fijas de stop loss y take profit.

## Detalles

- **Criterios de entrada**: El precio rompe las bandas del filtro de rango.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Stop loss o take profit.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `SamplingPeriod` = 100
  - `RangeMultiplier` = 3
  - `RiskPoints` = 50
  - `RewardPoints` = 100
  - `MaxTradesPerDay` = 5
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Range filter
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
