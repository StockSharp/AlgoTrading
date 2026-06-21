# Estrategia TrendGuard Flag Finder
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

TrendGuard Flag Finder detecta patrones de banderas alcistas y bajistas confirmadas por SuperTrend.
Compra cuando el precio rompe por encima de una bandera alcista y vende cuando rompe por debajo de una bandera bajista.

## Detalles

- **Criterios de entrada**: Ruptura de bandera con confirmación de SuperTrend
- **Largo/Corto**: Configurable
- **Criterios de salida**: Ruptura opuesta de bandera
- **Stops**: No
- **Valores predeterminados**:
  - `TradingDirection` = Both
  - `SuperTrend Length` = 10
  - `SuperTrend Factor` = 4
  - `MaxFlagDepth` = 5
  - `MinFlagLength` = 3
  - `MaxFlagLength` = 7
  - `MaxFlagRally` = 5
  - `MinBearFlagLength` = 3
  - `MaxBearFlagLength` = 7
  - `PoleMin` = 3
  - `PoleLength` = 7
  - `PoleMinBear` = 3
  - `PoleLengthBear` = 7
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Configurable
  - Indicadores: SuperTrend, Lowest, Highest
  - Stops: No
  - Complejidad: Avanzado
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
