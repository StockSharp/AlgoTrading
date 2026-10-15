# Estrategia de Spread de Alto Rendimiento con Filtro SMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera según el nivel del spread de crédito de alto rendimiento o el índice VIX. Cuando el spread seleccionado supera un umbral y el precio se encuentra en el lado adecuado de una media móvil simple, la estrategia abre una posición en la dirección elegida.

Las posiciones se mantienen durante un número fijo de velas antes de cerrarse. La estrategia opera únicamente con velas diarias.

## Detalles

- **Criterios de entrada**:
  - **Largo**: spread > umbral y (precio > SMA si el filtro está habilitado)
  - **Corto**: spread < umbral y (precio < SMA si el filtro está habilitado)
- **Largo/Corto**: un lado a la vez (parámetro)
- **Criterios de salida**: posición cerrada después del período de mantenimiento
- **Stops**: No
- **Valores predeterminados**:
  - `Basis` = HighYieldSpread
  - `Threshold` = 5
  - `IsLong` = true
  - `HoldingPeriod` = 5
  - `UseSmaFilter` = true
  - `SmaLength` = 50
  - `CandleType` = 1 day
- **Filtros**:
  - Categoría: Spread
  - Dirección: Configurable
  - Indicadores: SMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
