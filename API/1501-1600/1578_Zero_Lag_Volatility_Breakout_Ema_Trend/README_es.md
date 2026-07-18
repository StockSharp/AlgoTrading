# Estrategia Zero-lag de Ruptura de Volatilidad con EMA Tendencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Sistema de ruptura que usa la diferencia de EMA sin rezago con bandas de Bollinger y un filtro de tendencia EMA. Opcionalmente mantiene posiciones hasta una señal opuesta.

## Detalles

- **Criterios de entrada**: Dif cruza por encima de la banda superior con filtro de pendiente EMA.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Salida opcional en cruce de la banda media.
- **Stops**: Sin stops explícitos.
- **Valores predeterminados**:
  - `EmaLength` = 200
  - `StdMultiplier` = 2m
  - `UseBinary` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: EMA, Bollinger Bands
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
