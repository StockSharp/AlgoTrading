# Estrategia Simple de Cruce de EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia utiliza un cruce de dos medias móviles exponenciales con stop-loss y take-profit integrados.

Compra cuando la EMA rápida cruza por encima de la EMA lenta y vende cuando cruza por debajo.

## Detalles

- **Criterios de entrada**: Cruce de la EMA rápida con la EMA lenta.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Cruce opuesto u órdenes stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `Periods` = 17
  - `StopLoss` = 31 (absoluto)
  - `TakeProfit` = 69 (absoluto)
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: EMA
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
