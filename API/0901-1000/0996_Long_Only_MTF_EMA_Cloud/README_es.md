# Solo Largos MTF EMA Nube
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de cruce de nube EMA que opera en largo cuando la EMA corta cruza hacia arriba la EMA larga. Utiliza stop loss y take profit con porcentaje fijo.

## Detalles

- **Criterios de entrada**: La EMA corta cruza hacia arriba la EMA larga.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: El precio alcanza el stop loss o el take profit.
- **Stops**: Stop loss y take profit con porcentaje fijo.
- **Valores predeterminados**:
  - `CandleType` = TimeSpan.FromMinutes(1)
  - `ShortLength` = 21
  - `LongLength` = 50
  - `StopLossPercent` = 1.0m
  - `TakeProfitPercent` = 2.0m
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo
  - Indicadores: EMA
  - Stops: Sí
  - Complejidad: Principiante
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
