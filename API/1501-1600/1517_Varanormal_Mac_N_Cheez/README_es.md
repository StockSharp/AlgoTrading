# Estrategia Varanormal Mac N Cheez
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de cruce de SMA con stop trailing y objetivo de beneficio diario.

## Detalles

- **Criterios de entrada**:
  - **Largo**: La SMA rápida cruza por encima de la SMA lenta.
  - **Corto**: La SMA rápida cruza por debajo de la SMA lenta.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**:
  - Stop trailing o stop loss fijo.
  - El objetivo de beneficio diario cierra todas las posiciones.
- **Stops**: Sí, fijo y trailing.
- **Valores predeterminados**:
  - `FastLength` = 9
  - `SlowLength` = 21
  - `DailyTarget` = 200
  - `StopLossAmount` = 100
  - `TrailOffset` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: SMA
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
