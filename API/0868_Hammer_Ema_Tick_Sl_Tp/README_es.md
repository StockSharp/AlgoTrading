# Estrategia Hammer + EMA con SL/TP basado en ticks
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Combina los patrones de velas Hammer y Hammer invertido con un filtro de tendencia EMA y gestión de riesgo basada en ticks.

## Detalles

- **Criterios de entrada**: Hammer por encima de la EMA o Hammer invertido por debajo de la EMA.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Take profit o stop loss basado en ticks.
- **Stops**: Basado en ticks.
- **Valores predeterminados**:
  - `EmaLength` = 50
  - `StopLossTicks` = 1
  - `TakeProfitTicks` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Ambos
  - Indicadores: EMA, Hammer, Hammer invertido
  - Stops: Basado en ticks
  - Complejidad: Básico
  - Marco temporal: Intradía (1m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
